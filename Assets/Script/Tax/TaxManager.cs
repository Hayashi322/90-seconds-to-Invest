using UnityEngine;

using Unity.Netcode;

public class TaxManager : NetworkBehaviour

{

    public static TaxManager Instance;

    [Range(0f, 1f)] public float taxRate = 0.10f;  // อัตราภาษีคิดจากเงินปัจจุบัน

    public NetworkVariable<float> unpaidTax =

        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()

    {

        if (Instance == null) Instance = this;

    }

    public override void OnNetworkSpawn()

    {

        base.OnNetworkSpawn();

        // ให้แต่ละ client ถือ Instance ของ "TaxManager บนตัวเอง"

        if (IsOwner) Instance = this;

    }

    // === เรียกจาก client ของผู้เล่นคนนั้น เพื่อให้ server คำนวณภาษีของ "ผู้เรียก" ===

    [ServerRpc(RequireOwnership = false)]

    public void CalculateTaxServerRpc(ServerRpcParams rpc = default)

    {

        var clientId = rpc.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (!playerObj) return;

        var inv = playerObj.GetComponent<InventoryManager>();

        var tax = playerObj.GetComponent<TaxManager>();

        if (!inv || !tax) return;

        float taxThisTurn = Mathf.Max(0f, inv.cash.Value * taxRate);

        tax.unpaidTax.Value += taxThisTurn;

    }

    // === จ่ายภาษีทั้งหมดของผู้เรียก ===

    [ServerRpc(RequireOwnership = false)]

    public void PayTaxServerRpc(ServerRpcParams rpc = default)

    {

        var clientId = rpc.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (!playerObj) return;

        var inv = playerObj.GetComponent<InventoryManager>();

        var tax = playerObj.GetComponent<TaxManager>();

        if (!inv || !tax) return;

        float due = tax.unpaidTax.Value;

        if (due <= 0f || inv.cash.Value < due) return;

        inv.cash.Value -= due;

        tax.unpaidTax.Value = 0f;

    }

}
