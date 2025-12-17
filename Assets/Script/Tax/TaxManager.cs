using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TaxManager : NetworkBehaviour
{
    public static TaxManager Instance;

    // ============================================================
    // Settings
    // ============================================================
    [Header("Penalty Settings")]
    [SerializeField] private float taxPenaltyRate = 0.5f; // 50% penalty


    // ============================================================
    // Networked Output (UI อ่าน)
    // ============================================================
    public NetworkVariable<double> unpaidTax =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> taxableBase =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> effectiveRate =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    // ============================================================
    // Unity Lifecycle
    // ============================================================
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }


    // ============================================================
    // Helper (Server)
    // ============================================================
    public static bool AnyPlayerHasUnpaidTax()
    {
        if (!NetworkManager.Singleton.IsServer)
            return false;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var tax = kv.Value.PlayerObject?.GetComponent<TaxManager>();
            if (tax != null && tax.unpaidTax.Value > 0.01f)
                return true;
        }

        return false;
    }

    public static List<ulong> GetPlayersWithUnpaidTax()
    {
        var list = new List<ulong>();

        if (!NetworkManager.Singleton.IsServer)
            return list;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var tax = kv.Value.PlayerObject?.GetComponent<TaxManager>();
            if (tax != null && tax.unpaidTax.Value > 0.01f)
                list.Add(kv.Key);
        }

        return list;
    }


    // ============================================================
    // Progressive Tax Calculation
    // ============================================================
    private double CalcProgressiveTax(double baseAmount, out double effRate)
    {
        double taxable = Math.Max(0, baseAmount);

        double[] caps =
        {
            150_000,
            300_000,
            500_000,
            750_000,
            1_000_000,
            2_000_000,
            5_000_000
        };

        double[] rates =
        {
            0.00,
            0.05,
            0.10,
            0.15,
            0.20,
            0.25,
            0.30,
            0.35
        };

        double prevCap = 0;
        double tax = 0;

        for (int i = 0; i < caps.Length && taxable > 0; i++)
        {
            double span = caps[i] - prevCap;
            double use = Math.Min(taxable, span);

            tax += use * rates[i];
            taxable -= use;
            prevCap = caps[i];
        }

        if (taxable > 0)
            tax += taxable * rates[rates.Length - 1];

        effRate = baseAmount <= 0 ? 0 : tax / baseAmount;
        return tax;
    }


    // ============================================================
    // Calculate Tax (Phase 3)
    // ============================================================
    [ServerRpc(RequireOwnership = false)]
    public void CalculateTaxThisPhaseServerRpc(ServerRpcParams rpc = default)
    {
        var playerObj = NetworkManager.Singleton
            .ConnectedClients[rpc.Receive.SenderClientId]
            .PlayerObject;

        var inv = playerObj.GetComponent<InventoryManager>();
        var tax = playerObj.GetComponent<TaxManager>();

        if (!inv || !tax)
            return;

        double baseAmount = Math.Max(0, inv.cash.Value);

        double eff;
        double due = CalcProgressiveTax(baseAmount, out eff);

        tax.unpaidTax.Value += due;
        tax.taxableBase.Value = baseAmount;
        tax.effectiveRate.Value = eff;
    }


    // ============================================================
    // Pay Tax (Player Click)
    // ============================================================
    [ServerRpc(RequireOwnership = false)]
    public void PayTaxServerRpc(ServerRpcParams rpc = default)
    {
        var playerObj = NetworkManager.Singleton
            .ConnectedClients[rpc.Receive.SenderClientId]
            .PlayerObject;

        var inv = playerObj.GetComponent<InventoryManager>();
        var tax = playerObj.GetComponent<TaxManager>();

        if (!inv || !tax)
            return;

        double due = tax.unpaidTax.Value;
        if (due <= 0)
            return;

        // ❗ เงินไม่พอ → ห้ามจ่าย
        if (inv.cash.Value < due)
            return;

        inv.cash.Value -= due;
        tax.unpaidTax.Value = 0;
    }


    // ============================================================
    // Force Pay (Tax Audit Event)
    // ============================================================
    public static void ForcePayWithPenalty(GameObject playerObj)
    {
        var tax = playerObj.GetComponent<TaxManager>();
        if (tax != null)
            tax.ApplyPenalty(playerObj);
    }

    private void ApplyPenalty(GameObject playerObj)
    {
        var inv = playerObj.GetComponent<InventoryManager>();
        if (!inv)
            return;

        double due = unpaidTax.Value;
        if (due <= 0)
            return;

        double penalty = due * taxPenaltyRate;
        double total = due + penalty;

        // ❗ เงินไม่ติดลบ
        inv.cash.Value = Math.Max(0, inv.cash.Value - total);
        unpaidTax.Value = 0;
    }
}
