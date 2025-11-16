using Unity.Netcode;
using UnityEngine;

public class PlayerLotteryState : NetworkBehaviour
{
    // ซื้อหวยได้ 1 ครั้งต่อเกม
    public NetworkVariable<bool> HasTicket = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // เลขหวยที่ถือ (-1 = ยังไม่มี)
    public NetworkVariable<int> TicketNumber = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private InventoryManager inv;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer || IsOwner)
        {
            inv = GetComponent<InventoryManager>();
            if (inv == null)
                inv = InventoryManager.Instance;
        }

        Debug.Log($"[Lottery] PlayerLotteryState spawned for client {OwnerClientId}");
    }

    /// <summary>
    /// ให้ client เจ้าของตัวเองเรียกเวลาอยากซื้อหวยใบที่ slotIndex (0..11)
    /// เช่นจากปุ่มใน UI ร้านขายหวย
    /// </summary>
    [ServerRpc]
    public void BuyTicketAtIndexServerRpc(int slotIndex)
    {
        if (!IsServer) return;

        if (LotteryManager.Instance == null)
        {
            Debug.LogError("[Lottery] LotteryManager not found.");
            return;
        }

        LotteryManager.Instance.ServerBuyTicket(OwnerClientId, slotIndex);
    }

    /// <summary>
    /// Helper ให้ UI ใช้อ่านเลขที่ถือ (ถ้ายังไม่มีจะเป็น -1)
    /// </summary>
    public int GetMyTicketNumber() => TicketNumber.Value;
}
