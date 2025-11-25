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
    /// สุ่ม 2 Event โดย "ไม่ให้ซ้ำประเภทกัน" เช่น ทองขึ้น + ทองลง จะไม่ออกพร้อมกัน
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

        int safety = 100; // กันลูปไม่จบ ถ้า config ไม่พอ

        while (currentEventIndices.Count < 2 &&
               used.Count < allEvents.Length &&
               safety-- > 0)
        {
            int idx = UnityEngine.Random.Range(0, allEvents.Length);

            // กันสุ่ม index เดิมซ้ำ
            if (!used.Add(idx))
                continue;

            var candidate = allEvents[idx];
            if (candidate == null) continue;

            // ---- เช็กว่า "ชนประเภท" กับที่เลือกไปแล้วหรือไม่ ----
            bool conflict = false;

            foreach (int existingIdx in currentEventIndices)
            {
                var existing = allEvents[existingIdx];
                if (existing == null) continue;

                if (IsSameMarketCategory(existing, candidate))
                {
                    // เช่น ทั้งคู่มี target = Gold หรือ ทั้งคู่มี StocksTech
                    conflict = true;
                    break;
                }
            }

            if (conflict)
            {
                // ข้ามตัวนี้ไป หาตัวใหม่
                continue;
            }

            // ถ้าไม่ conflict → ใช้งานได้
            currentEventIndices.Add(idx);
            ApplyEventEffects(candidate);
        }

        Debug.Log($"[EventManagerNet] Rolled events: {string.Join(",", currentEventIndices)}");
    }

    /// <summary>
    /// ใช้เช็กว่า event สองอันเป็น "ประเภทตลาดเดียวกัน" ไหม
    /// เช่น ทั้งคู่ไปยุ่งกับ Gold, หรือทั้งคู่เป็น StocksTech เป็นต้น
    /// </summary>
    private bool IsSameMarketCategory(EventConfig a, EventConfig b)
    {
        if (a == null || b == null) return false;

        foreach (var ea in a.effects)
        {
            foreach (var eb in b.effects)
            {
                // ถ้า target เหมือนกัน และเป็นประเภทตลาดหลัก ๆ ที่เราอยากกันไม่ให้ซ้ำ
                if (ea.target == eb.target &&
                    (ea.target == MarketTarget.Gold ||
                     ea.target == MarketTarget.RealEstate ||
                     ea.target == MarketTarget.StocksAll ||
                     ea.target == MarketTarget.StocksTech ||
                     ea.target == MarketTarget.StocksTourism))
                {
                    return true;
                }
            }
        }
        return false;
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
        if (!IsServer || cfg == null) return;

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

                    // target พิเศษอย่าง ภาษี / คาสิโน ให้ไปจัดการในระบบอื่น
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
