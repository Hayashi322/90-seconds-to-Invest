using UnityEngine;
using Unity.Netcode;

public class TaxManager : NetworkBehaviour
{
    public static TaxManager Instance;

    // Output ให้ UI อ่าน
    public NetworkVariable<float> unpaidTax =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> taxableBase =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> effectiveRate =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); // 0..1

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner) Instance = this; // อ้างอิง TaxManager ของ local player
    }

    // ===== ภาษีแบบขั้นบันไดไทย (ง่ายสำหรับเกม) =====
    // ใช้ "เงินสดปัจจุบัน" เป็น proxy ของรายได้สุทธิรอบนี้ เพื่อเรียนรู้แนวคิดภาษี
    private float CalcProgressiveTax(float baseAmount, out float effRate)
    {
        float taxable = Mathf.Max(0f, baseAmount);

        // เพดานรายได้ต่อชั้น (บาท)
        float[] caps = { 150_000f, 300_000f, 500_000f, 750_000f, 1_000_000f, 2_000_000f, 5_000_000f };
        // อัตราภาษีต่อชั้น
        float[] rates = { 0.00f, 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.35f };

        float prevCap = 0f;
        float tax = 0f;

        for (int i = 0; i < caps.Length && taxable > 0f; i++)
        {
            float span = caps[i] - prevCap;         // ช่วงกว้างของชั้นนี้
            float use = Mathf.Min(taxable, span);  // ส่วนที่ตกในชั้นนี้จริง
            tax += use * rates[i];
            taxable -= use;
            prevCap = caps[i];
        }

        if (taxable > 0f)                           // ส่วนที่เกิน 5M
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

        float baseAmount = Mathf.Max(0f, inv.cash.Value); // proxy รายได้
        float eff;
        float due = CalcProgressiveTax(baseAmount, out eff);

        taxMgr.taxableBase.Value = baseAmount;
        taxMgr.effectiveRate.Value = eff;
        taxMgr.unpaidTax.Value = due;
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

        if (Timer.Instance == null || Timer.Instance.Phase != 3) return; // กันกดนอกเฟส

        float due = taxMgr.unpaidTax.Value;
        if (due <= 0f || inv.cash.Value < due) return;

        inv.cash.Value -= due;
        taxMgr.unpaidTax.Value = 0f; // ชำระแล้ว
    }
}
