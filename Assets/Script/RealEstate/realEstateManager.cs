using System;
using Unity.Netcode;
using UnityEngine;

public class RealEstateManager : NetworkBehaviour
{
    public static RealEstateManager Instance { get; private set; }

    [Header("Initial Prices (Baht)")]
    [SerializeField] private int[] initialPrices = new int[] { 8_000_000, 10_000_000, 12_000_000, 15_000_000 };

    [Header("Price Tick (sec)")]
    [SerializeField] private float updateInterval = 10f;

    [Header("Clamp Price")]
    [SerializeField] private int minPrice = 5_000_000;
    [SerializeField] private int maxPrice = 20_000_000;

    // ✅ ราคาต่ำสุดที่ยอมให้ลงได้เมื่อโดนอีเว้นหนัก ๆ (เช่น ฟองสบู่แตก)
    [SerializeField] private int eventMinPrice = 1_000_000;

    [Header("Volatility (±k per tick)")]
    [SerializeField] private int tickK = 50;

    [Serializable]
    public struct HouseRecordNet : INetworkSerializable, IEquatable<HouseRecordNet>
    {
        public int price;          // ราคา "ที่เห็น" = base * multiplier
        public ulong ownerClientId; // ulong.MaxValue = none
        public bool forRent;       // ปล่อยเช่า/ไม่ปล่อย

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref price);
            s.SerializeValue(ref ownerClientId);
            s.SerializeValue(ref forRent);
        }

        public bool Equals(HouseRecordNet other) =>
            price == other.price && ownerClientId == other.ownerClientId && forRent == other.forRent;
    }

    public NetworkList<HouseRecordNet> Houses { get; private set; }

    // ราคา "ฐาน" (ไม่โดน Event คูณ) ใช้เฉพาะฝั่ง Server
    private int[] basePrices;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Houses = new NetworkList<HouseRecordNet>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Houses.Clear();

            int count = initialPrices.Length;
            basePrices = new int[count];

            for (int i = 0; i < count; i++)
            {
                basePrices[i] = Mathf.Clamp(initialPrices[i], minPrice, maxPrice);

                Houses.Add(new HouseRecordNet
                {
                    price = basePrices[i],
                    ownerClientId = ulong.MaxValue,
                    forRent = false
                });
            }

            InvokeRepeating(nameof(ServerTickUpdatePricesAndRent), 3f, updateInterval);
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            CancelInvoke(nameof(ServerTickUpdatePricesAndRent));
        }

        if (Instance == this)
            Instance = null;
    }

    private void ServerTickUpdatePricesAndRent()
    {
        if (!IsServer) return;
        if (Houses.Count == 0 || basePrices == null) return;

        // อ่านตัวคูณอสังหาฯ จาก Event (ไม่มี Event = 1)
        float eventMul = 1f;
        if (EventManagerNet.Instance != null)
            eventMul = EventManagerNet.Instance.GetRealEstateMultiplier();

        for (int i = 0; i < Houses.Count; i++)
        {
            var h = Houses[i];

            // 1) อัปเดต "base price" แบบสุ่มปกติ
            int delta = UnityEngine.Random.Range(-tickK, tickK + 1) * 1_000;
            basePrices[i] = Mathf.Clamp(basePrices[i] + delta, minPrice, maxPrice);

            // 2) คำนวณราคาโชว์จริงจาก base * eventMultiplier
            int rawPrice = Mathf.RoundToInt(basePrices[i] * eventMul);

            // ✅ ถ้า eventMul ต่ำมาก (เช่น ฟองสบู่แตก 0.2, 0.3)
            // ให้ใช้ eventMinPrice เป็นพื้นแทน minPrice ปกติ
            int lowerBound = (eventMul < 0.5f) ? eventMinPrice : minPrice;

            int finalPrice = Mathf.Clamp(rawPrice, lowerBound, maxPrice);
            h.price = finalPrice;

            // 3) จ่ายค่าเช่า
            if (h.ownerClientId != ulong.MaxValue && h.forRent &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(h.ownerClientId, out var cc) &&
                cc.PlayerObject)
            {
                var inv = cc.PlayerObject.GetComponent<InventoryManager>();
                if (inv)
                    inv.cash.Value += h.price / 100f; // 1% ต่อ tick จากราคาโชว์
            }

            Houses[i] = h; // sync
        }
    }

    // ===== ซื้อ / ขาย / ปล่อยเช่า =====

    [ServerRpc(RequireOwnership = false)]
    public void BuyHouseServerRpc(int index, ServerRpcParams rpc = default)
    {
        if (!IsServer || index < 0 || index >= Houses.Count) return;

        var clientId = rpc.Receive.SenderClientId;
        var rec = Houses[index];
        if (rec.ownerClientId != ulong.MaxValue) return; // มีเจ้าของแล้ว

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc) || !cc.PlayerObject) return;
        var inv = cc.PlayerObject.GetComponent<InventoryManager>();
        if (inv == null || inv.cash.Value < rec.price) return;

        inv.cash.Value -= rec.price;
        rec.ownerClientId = clientId;
        rec.forRent = false; // เริ่มต้นยังไม่ปล่อยเช่า
        Houses[index] = rec;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SellHouseServerRpc(int index, ServerRpcParams rpc = default)
    {
        if (!IsServer || index < 0 || index >= Houses.Count) return;

        var clientId = rpc.Receive.SenderClientId;
        var rec = Houses[index];
        if (rec.ownerClientId != clientId) return; // ต้องเป็นเจ้าของ

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc) || !cc.PlayerObject) return;
        var inv = cc.PlayerObject.GetComponent<InventoryManager>();
        if (inv == null) return;

        inv.cash.Value += rec.price;
        rec.ownerClientId = ulong.MaxValue;
        rec.forRent = false;
        Houses[index] = rec;
    }

    // ✅ ปล่อยเช่า/ยกเลิกปล่อยเช่า
    [ServerRpc(RequireOwnership = false)]
    public void ForRentServerRpc(int index, bool enable, ServerRpcParams rpc = default)
    {
        if (!IsServer || index < 0 || index >= Houses.Count) return;

        var clientId = rpc.Receive.SenderClientId;
        var rec = Houses[index];

        // ต้องเป็นเจ้าของถึงจะสลับสถานะได้
        if (rec.ownerClientId != clientId) return;

        rec.forRent = enable;
        Houses[index] = rec;
    }
}
