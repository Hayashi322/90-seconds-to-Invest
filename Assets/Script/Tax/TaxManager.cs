using System;
using System.Collections.Generic;          // ใช้ List<>
using Unity.Netcode;
using UnityEngine;

public class TaxManager : NetworkBehaviour
{
    public static TaxManager Instance;

    // Output ให้ UI อ่าน
    public NetworkVariable<double> unpaidTax =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> taxableBase =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<double> effectiveRate =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 0..1

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ตัวที่เป็น local player ให้เก็บเป็น Instance สำหรับ UI ฝั่งนั้นใช้
        if (IsOwner)
            Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ============================================================
    //   ฟังก์ชันช่วยฝั่ง Server สำหรับ EventManagerNet
    //   - เช็กว่ามี player ไหนค้างภาษีไหม
    //   - ดึงรายชื่อ player ที่ค้างภาษี
    // ============================================================

    /// <summary>
    /// ใช้บน Server: true ถ้ามีคนค้างภาษีอย่างน้อย 1 คน
    /// </summary>
    public static bool AnyPlayerHasUnpaidTax()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return false;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kv.Value.PlayerObject;
            if (!playerObj) continue;

            var tax = playerObj.GetComponent<TaxManager>();
            if (tax == null) continue;

            if (tax.unpaidTax.Value > 0.01f)
                return true;
        }

        return false;
    }

    /// <summary>
    /// ใช้บน Server: คืน List ของ clientId ที่ยังมีภาษีค้างชำระ
    /// </summary>
    public static List<ulong> GetPlayersWithUnpaidTax()
    {
        var result = new List<ulong>();

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return result;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kv.Key;
            var playerObj = kv.Value.PlayerObject;
            if (!playerObj) continue;

            var tax = playerObj.GetComponent<TaxManager>();
            if (tax == null) continue;

            if (tax.unpaidTax.Value > 0.01f)
                result.Add(clientId);
        }

        return result;
    }

    // ============================================================
    //   คำนวณภาษีแบบขั้นบันได
    // ============================================================

    // ภาษีแบบขั้นบันไดไทย (เวอร์ชันง่ายสำหรับเกม)
    // ใช้ "เงินสดปัจจุบัน" เป็น proxy ของรายได้สุทธิรอบนี้ เพื่อเรียนรู้แนวคิดภาษี
    private double CalcProgressiveTax(double baseAmount, out double effRate)
    {
        double taxable = Math.Max(0f, baseAmount);

        // เพดานรายได้ต่อชั้น (บาท)
        double[] caps =
        {
            150_000f, 300_000f, 500_000f, 750_000f,
            1_000_000f, 2_000_000f, 5_000_000f
        };

        // อัตราภาษีต่อชั้น
        double[] rates =
        {
            0.00f, 0.05f, 0.10f, 0.15f,
            0.20f, 0.25f, 0.30f, 0.35f
        };

        double prevCap = 0f;
        double tax = 0f;

        for (int i = 0; i < caps.Length && taxable > 0f; i++)
        {
            double span = caps[i] - prevCap;       // ช่วงกว้างของชั้นนี้
            double use = Math.Min(taxable, span);  // ส่วนที่ตกในชั้นนี้จริง
            tax += use * rates[i];
            taxable -= use;
            prevCap = caps[i];
        }

        // ส่วนที่เกิน 5M
        if (taxable > 0f)
            tax += taxable * rates[rates.Length - 1];

        effRate = (baseAmount <= 0f) ? 0f : (tax / baseAmount);
        return tax;
    }

    // เรียกตอนเข้า Phase 3 เพื่อคำนวณบิลภาษีของผู้เล่นคนนั้น
    [ServerRpc(RequireOwnership = false)]
    public void CalculateTaxThisPhaseServerRpc(ServerRpcParams rpc = default)
    {
        var clientId = rpc.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (!playerObj) return;

        var inv = playerObj.GetComponent<InventoryManager>();
        var taxMgr = playerObj.GetComponent<TaxManager>();
        if (!inv || !taxMgr) return;

        double baseAmount = Math.Max(0f, inv.cash.Value); // proxy รายได้
        double eff;

        // คิดภาษีของรอบนี้
        double currentDue = CalcProgressiveTax(baseAmount, out eff);

        // ดึงยอดค้างเก่ามาบวกกับยอดใหม่ → กลายเป็นยอดค้างสะสม
        double oldDue = taxMgr.unpaidTax.Value;
        taxMgr.unpaidTax.Value = oldDue + currentDue;

        // เก็บฐานที่ใช้คิดและ effective rate ของ "รอบล่าสุด"
        taxMgr.taxableBase.Value = baseAmount;
        taxMgr.effectiveRate.Value = eff;
    }

    // กดชำระภาษี — อนุญาตเฉพาะ Phase 3
    [ServerRpc(RequireOwnership = false)]
    public void PayTaxServerRpc(ServerRpcParams rpc = default)
    {
        var clientId = rpc.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (!playerObj) return;

        var inv = playerObj.GetComponent<InventoryManager>();
        var taxMgr = playerObj.GetComponent<TaxManager>();
        if (!inv || !taxMgr) return;

        // กันไม่ให้จ่ายนอก Phase 3
        if (Timer.Instance == null || Timer.Instance.Phase != 3) return;

        double due = taxMgr.unpaidTax.Value;
        if (due <= 0f || inv.cash.Value < due) return;

        inv.cash.Value -= due;
        taxMgr.unpaidTax.Value = 0f; // ชำระแล้ว เคลียร์หนี้
    }
}
