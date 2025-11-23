using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class LotteryManager : NetworkBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int ticketPrice = 120;
    [SerializeField] private int ticketsPerGame = 12;

    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI InvTicketNumber;
    [SerializeField] private CanvasGroup CanvasGroup;

    // ✅ หวยทั้งหมดในร้าน
    public NetworkList<LotterySlotNet> Slots = new NetworkList<LotterySlotNet>();

    // ✅ เลขถูกรางวัล 1 ใบต่อเกม
    private NetworkVariable<int> winningTicketNumber = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int TicketPrice => ticketPrice;
    public int WinningTicketNumber => winningTicketNumber.Value;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            GenerateTicketsForGame();
    }

    /// ✅ สุ่มหวย + สุ่มใบถูกรางวัล 1 ใบ
    private void GenerateTicketsForGame()
    {
        Slots.Clear();
        var usedNumbers = new HashSet<int>();

        int winningIndex = UnityEngine.Random.Range(0, ticketsPerGame);

        for (int i = 0; i < ticketsPerGame; i++)
        {
            int number;
            do
            {
                number = UnityEngine.Random.Range(0, 1_000_000);
            }
            while (usedNumbers.Contains(number));

            usedNumbers.Add(number);

            var slot = new LotterySlotNet
            {
                ticketNumber = number,
                isSold = false,
                ownerClientId = 0,
                isWinning = (i == winningIndex)   // ✅ ใบนี้คือใบที่ถูกรางวัล
            };

            Slots.Add(slot);
        }

        winningTicketNumber.Value = Slots[winningIndex].ticketNumber;

        Debug.Log($"[Lottery] ✅ Winning Ticket = {winningTicketNumber.Value:000000}");
    }

    /// ✅ ซื้อหวย
    public void ServerBuyTicket(ulong buyerClientId, int slotIndex)
    {
        if (!IsServer) return;

        if (slotIndex < 0 || slotIndex >= Slots.Count) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(buyerClientId, out var client)) return;

        var playerObj = client.PlayerObject;
        if (!playerObj) return;

        var inv = playerObj.GetComponent<InventoryManager>();
        var lotto = playerObj.GetComponent<PlayerLotteryState>();

        if (inv == null || lotto == null) return;

        var slot = Slots[slotIndex];

        // ✅ ซื้อไม่ได้
        if (slot.isSold) return;
        if (lotto.HasTicket.Value) return;
        if (inv.cash.Value < ticketPrice) return;

        // ✅ ซื้อสำเร็จ
        inv.cash.Value -= ticketPrice;
        slot.isSold = true;
        slot.ownerClientId = buyerClientId;

        // ✅ ลบออกจากร้าน
        Slots.RemoveAt(slotIndex);

        lotto.HasTicket.Value = true;
        lotto.TicketNumber.Value = slot.ticketNumber;

        if (InvTicketNumber)
            InvTicketNumber.text = $"{lotto.TicketNumber.Value:000000}";

        if (CanvasGroup)
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }

        Debug.Log($"[Lottery] ✅ Client {buyerClientId} bought ticket {slot.ticketNumber:000000}");
    }

    /// ✅ ให้ระบบสรุปผลเช็คว่าถูกหวยไหม
    public bool HasWinningTicket(int number)
    {
        return number == winningTicketNumber.Value;
    }
}
