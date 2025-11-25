using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EventManagerNet : NetworkBehaviour
{
    public static EventManagerNet Instance { get; private set; }

    [Header("Event Config ทั้งหมด (ตั้งใน Inspector)")]
    [SerializeField] private EventConfig[] allEvents;

    [Header("รอบสุดท้ายสำหรับอีเว้นตรวจภาษี")]
    [SerializeField] private int finalRoundForTaxAudit = 3;

    public NetworkList<int> currentEventIndices;

    private float goldMultiplier = 1f;
    private float realEstateMultiplier = 1f;

    private Dictionary<string, float> stockMultipliers =
        new Dictionary<string, float>();

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
        currentEventIndices.OnListChanged += _ => OnEventsChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


    // =============================================================
    //  MAIN ROLL — ONLY 1 EVENT PER ROUND
    // =============================================================

    public void RollEventsForThisTurn()
    {
        if (!IsServer) return;

        ResetMultipliers();
        currentEventIndices.Clear();

        // 1) Check special TaxAudit event
        if (TryInjectTaxAuditEvent())
            return;  // ถ้ามี TaxAudit แล้ว จบรอบนี้เลย

        // 2) Normal event (ONLY 1 EVENT)
        int idx = GetRandomNonConflictEvent();
        if (idx >= 0)
        {
            currentEventIndices.Add(idx);
            ApplyEventEffects(allEvents[idx]);
        }

        Debug.Log($"[EventManagerNet] Rolled event: {string.Join(",", currentEventIndices)}");
    }


    // =============================================================
    //  SPECIAL — TAX AUDIT EVENT (ตรวจภาษี)
    // =============================================================

    private bool TryInjectTaxAuditEvent()
    {
        int taxIdx = Array.FindIndex(allEvents, e => e != null && e.id == GameEventId.TaxAudit);
        if (taxIdx < 0) return false;

        var cfg = allEvents[taxIdx];

        // ต้องเป็นรอบสุดท้าย (หรือรอบที่กำหนดใน Inspector)
        if (cfg.onlyFinalRound)
        {
            if (Timer.Instance == null) return false;
            if (Timer.Instance.Round != finalRoundForTaxAudit) return false;
        }

        // ต้องมีผู้เล่นค้างภาษี
        if (!TaxManager.AnyPlayerHasUnpaidTax())
            return false;

        // โอกาส 50%
        if (UnityEngine.Random.value > 0.5f)
            return false;

        // ผ่านทุกเงื่อนไข → ใช้อีเว้นนี้
        currentEventIndices.Add(taxIdx);

        var debtPlayers = TaxManager.GetPlayersWithUnpaidTax();
        foreach (var pid in debtPlayers)
        {
            ForceOpenTaxUIClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new List<ulong>() { pid }
                }
            });
        }

        return true;
    }


    [ClientRpc]
    private void ForceOpenTaxUIClientRpc(ClientRpcParams rpc = default)
    {
        var ui = FindObjectOfType<TaxUI>();
        if (ui != null) ui.OpenForceMode();
    }


    // =============================================================
    //  RANDOM EVENT PICKER (ONE EVENT ONLY)
    // =============================================================

    private int GetRandomNonConflictEvent()
    {
        List<int> candidate = new List<int>();

        for (int i = 0; i < allEvents.Length; i++)
        {
            var cfg = allEvents[i];
            if (cfg == null) continue;

            if (cfg.id == GameEventId.TaxAudit) continue; // TaxAudit ไม่สุ่ม
            candidate.Add(i);
        }

        if (candidate.Count == 0) return -1;

        return candidate[UnityEngine.Random.Range(0, candidate.Count)];
    }


    // =============================================================
    //  MULTIPLIERS
    // =============================================================

    private void ResetMultipliers()
    {
        goldMultiplier = 1f;
        realEstateMultiplier = 1f;

        stockMultipliers.Clear();
        stockMultipliers["PTT"] = 1f;
        stockMultipliers["KBANK"] = 1f;
        stockMultipliers["AOT"] = 1f;
        stockMultipliers["BDMS"] = 1f;
        stockMultipliers["DELTA"] = 1f;
        stockMultipliers["CPNREIT"] = 1f;
    }

    private void ApplyEventEffects(EventConfig cfg)
    {
        if (cfg == null) return;

        foreach (var eff in cfg.effects)
        {
            switch (eff.target)
            {
                case MarketTarget.Gold:
                    goldMultiplier *= eff.multiplier;
                    break;

                case MarketTarget.RealEstate:
                    realEstateMultiplier *= eff.multiplier;
                    stockMultipliers["CPNREIT"] *= eff.multiplier;
                    break;

                case MarketTarget.StocksAll:
                    foreach (var k in new List<string>(stockMultipliers.Keys))
                        stockMultipliers[k] *= eff.multiplier;
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
                    foreach (var k in new List<string>(stockMultipliers.Keys))
                        stockMultipliers[k] *= eff.multiplier;
                    break;
            }
        }
    }


    // =============================================================
    //  PUBLIC GETTERS
    // =============================================================

    public float GetGoldMultiplier() => goldMultiplier;
    public float GetRealEstateMultiplier() => realEstateMultiplier;

    public float GetStockMultiplier(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return 1f;
        return stockMultipliers.TryGetValue(symbol, out var m) ? m : 1f;
    }

    public IReadOnlyList<EventConfig> GetCurrentEvents()
    {
        List<EventConfig> list = new List<EventConfig>();
        foreach (int idx in currentEventIndices)
        {
            if (idx >= 0 && idx < allEvents.Length)
                list.Add(allEvents[idx]);
        }
        return list;
    }
}
