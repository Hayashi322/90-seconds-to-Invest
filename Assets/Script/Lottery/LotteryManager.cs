using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LotteryManager : NetworkBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int ticketPrice = 120;      // ใบละ 120 ฿
    [SerializeField] private int ticketsPerGame = 12;    // หวยในร้าน 12 ใบ

    // หวยในร้าน (เลข + สถานะ)
    public NetworkList<LotterySlotNet> Slots = new NetworkList<LotterySlotNet>();

    public int TicketPrice => ticketPrice;

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
        {
            GenerateTicketsForGame();
        }
    }

    /// <summary>
    /// สุ่มหวย ticketsPerGame ใบ ใช้ตอนเริ่มเกม/โหลดฉาก
    /// </summary>
    private void GenerateTicketsForGame()
    {
        Slots.Clear();

        var usedNumbers = new HashSet<int>();

        for (int i = 0; i < ticketsPerGame; i++)
        {
            int number;
            do
            {
                // เลข 000000 - 999999
                number = Random.Range(0, 1_000_000);
            }
            while (usedNumbers.Contains(number));   // กันเลขซ้ำ

            usedNumbers.Add(number);

            var slot = new LotterySlotNet
            {
                ticketNumber = number,
                isSold = false,
                ownerClientId = 0
            };

            Slots.Add(slot);
        }

        Debug.Log($"[Lottery] Generated {ticketsPerGame} tickets for this game.");
    }

    /// <summary>
    /// ให้ PlayerLotteryState เรียก เมื่อผู้เล่นอยากซื้อใบที่ slotIndex
    /// </summary>
    public void ServerBuyTicket(ulong buyerClientId, int slotIndex)
    {
        if (!IsServer) return;

        if (slotIndex < 0 || slotIndex >= Slots.Count)
        {
            Debug.LogWarning($"[Lottery] Invalid slot index {slotIndex}");
            return;
        }

        // หา player + inventory + lottery ของคนซื้อ
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(buyerClientId, out var client))
        {
            Debug.LogError($"[Lottery] Buyer client {buyerClientId} not found.");
            return;
        }

        var playerObj = client.PlayerObject;
        if (!playerObj)
        {
            Debug.LogError($"[Lottery] Buyer {buyerClientId} has no PlayerObject.");
            return;
        }

        var inv = playerObj.GetComponent<InventoryManager>();
        var lotto = playerObj.GetComponent<PlayerLotteryState>();

        if (inv == null || lotto == null)
        {
            Debug.LogError("[Lottery] InventoryManager or PlayerLotteryState missing on player.");
            return;
        }

        var slot = Slots[slotIndex];

        // ====== เช็คเงื่อนไขห้ามซื้อ ======

        // 1) ใบนี้ถูกซื้อไปแล้ว
        if (slot.isSold)
        {
            Debug.Log($"[Lottery] Slot {slotIndex} already sold.");
            return;
        }

        // 2) คนนี้เคยซื้อหวยไปแล้ว (ซื้อได้ 1 ครั้งต่อเกม)
        if (lotto.HasTicket.Value)
        {
            Debug.Log($"[Lottery] Client {buyerClientId} already has a ticket.");
            return;
        }

        // 3) เงินไม่พอ
        if (inv.cash.Value < ticketPrice)
        {
            Debug.Log($"[Lottery] Client {buyerClientId} not enough cash (need {ticketPrice}, has {inv.cash.Value}).");
            return;
        }

        // ====== ซื้อได้ → หักเงิน + เซ็ตสถานะ ======

        // หักเงิน
        inv.cash.Value -= ticketPrice;

        // อัปเดต slot ในร้าน
        slot.isSold = true;
        slot.ownerClientId = buyerClientId;
        //Slots[slotIndex] = slot;   // ไม่ต้องแล้วถ้าจะลบออก

        Slots.RemoveAt(slotIndex);   // 👈 ลบใบนี้ออกจากร้านเลย


        // เซ็ตข้อมูลฝั่งผู้เล่น
        lotto.HasTicket.Value = true;
        lotto.TicketNumber.Value = slot.ticketNumber;

        Debug.Log($"[Lottery] Client {buyerClientId} bought ticket {slot.ticketNumber:000000} at slot {slotIndex}.");
    }
}
