using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EventManagerNet : NetworkBehaviour
{
    public static EventManagerNet Instance { get; private set; }

    [Header("Event Config ทั้งหมด (ตั้งใน Inspector)")]
    [SerializeField] private EventConfig[] allEvents;

    // เก็บ index ของ Event ที่สุ่มได้ใน "เทิร์นนี้" (sync ทุก client)
    public NetworkList<int> currentEventIndices;

    // ตัวคูณของเทิร์นนี้
    private float goldMultiplier = 1f;
    private float realEstateMultiplier = 1f;

    // multiplier แยกตามหุ้นแต่ละตัว (ใช้ชื่อ symbol เป็น key)
    private Dictionary<string, float> stockMultipliers =
        new Dictionary<string, float>();

    // UI ฝั่ง client subscribe ไว้ได้
    public event Action OnEventsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentEventIndices = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentEventIndices.OnListChanged += OnEventListChanged;
    }

    private void OnDestroy()
    {
        if (currentEventIndices != null)
            currentEventIndices.OnListChanged -= OnEventListChanged;

        if (Instance == this) Instance = null;
    }

    private void OnEventListChanged(NetworkListEvent<int> changeEvent)
    {
        OnEventsChanged?.Invoke();
    }

    //================= SERVER SIDE =================//

    /// <summary>
    /// เรียกเฉพาะ Server ตอนเข้า Phase 2 ของทุกเทิร์น
    /// </summary>
    public void RollEventsForThisTurn()
    {
        if (!IsServer) return;

        if (allEvents == null || allEvents.Length == 0)
        {
            Debug.LogWarning("[EventManagerNet] ไม่มี EventConfig เลย");
            return;
        }

        ResetMultipliers();

        currentEventIndices.Clear();
        HashSet<int> used = new HashSet<int>();

        while (currentEventIndices.Count < 2 && used.Count < allEvents.Length)
        {
            int idx = UnityEngine.Random.Range(0, allEvents.Length);
            if (used.Add(idx))
            {
                currentEventIndices.Add(idx);
                ApplyEventEffects(allEvents[idx]);
            }
        }

        Debug.Log($"[EventManagerNet] Rolled events: {string.Join(",", currentEventIndices)}");
    }

    private void ResetMultipliers()
    {
        if (!IsServer) return;

        goldMultiplier = 1f;
        realEstateMultiplier = 1f;
        stockMultipliers.Clear();

        // ตั้ง default ให้หุ้นทุกตัว = 1 (ชื่อเท่ากับใน StockMarketManager)
        stockMultipliers["PTT"] = 1f;
        stockMultipliers["KBANK"] = 1f;
        stockMultipliers["AOT"] = 1f;
        stockMultipliers["BDMS"] = 1f;
        stockMultipliers["DELTA"] = 1f;
        stockMultipliers["CPNREIT"] = 1f;
    }

    private void ApplyEventEffects(EventConfig cfg)
    {
        if (!IsServer) return;

        foreach (var eff in cfg.effects)
        {
            switch (eff.target)
            {
                case MarketTarget.Gold:
                    goldMultiplier *= eff.multiplier;
                    break;

                case MarketTarget.RealEstate:
                    realEstateMultiplier *= eff.multiplier;
                    // หุ้นอสังหาฯ ในตลาดหุ้นเราคือ CPNREIT
                    stockMultipliers["CPNREIT"] *= eff.multiplier;
                    break;

                case MarketTarget.StocksAll:
                    foreach (var key in new List<string>(stockMultipliers.Keys))
                        stockMultipliers[key] *= eff.multiplier;
                    break;

                case MarketTarget.StocksTech:
                    stockMultipliers["DELTA"] *= eff.multiplier;
                    break;

                case MarketTarget.StocksTourism:
                    stockMultipliers["AOT"] *= eff.multiplier;
                    break;

                case MarketTarget.Everything:
                    goldMultiplier *= eff.multiplier;
                    realEstateMultiplier *= eff.multiplier;
                    foreach (var key in new List<string>(stockMultipliers.Keys))
                        stockMultipliers[key] *= eff.multiplier;
                    break;

                    // ถ้ามี target ใหม่ (เช่น ภาษี, คาสิโน, เงินอุดหนุน)
                    // ให้ไปจัดการในระบบอื่น เช่น TaxManager / CasinoManager แยกต่างหาก
            }
        }
    }

    //================= PUBLIC GETTERS (Client/Server ใช้ร่วมกัน) =================//

    public float GetGoldMultiplier() => goldMultiplier;

    public float GetRealEstateMultiplier() => realEstateMultiplier;

    public float GetStockMultiplier(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return 1f;
        if (stockMultipliers.TryGetValue(symbol, out var m)) return m;
        return 1f;
    }

    // ใช้ให้ UI อ่านข้อมูลข่าว (รวม config ของข่าวที่สุ่มได้ในเทิร์นนี้)
    public IReadOnlyList<EventConfig> GetCurrentEvents()
    {
        List<EventConfig> list = new List<EventConfig>();
        if (allEvents == null) return list;

        foreach (int idx in currentEventIndices)
        {
            if (idx >= 0 && idx < allEvents.Length)
                list.Add(allEvents[idx]);
        }
        return list;
    }
}
