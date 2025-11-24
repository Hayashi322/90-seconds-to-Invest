using System;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class RealEstateManager : NetworkBehaviour
{
    [Header("Initial Prices (Baht)")]
    [SerializeField] private int[] initialPrices = new int[] { 8_000_000, 10_000_000, 12_000_000, 15_000_000 };

    [Header("Price Tick (sec)")]
    [SerializeField] private float updateInterval = 10f;

    [Header("Clamp Price")]
    [SerializeField] private int minPrice = 5_000_000;
    [SerializeField] private int maxPrice = 20_000_000;

    [Header("Volatility (±k per tick)")]
    [SerializeField] private int tickK = 50;



    [Serializable]
    public struct HouseRecordNet : INetworkSerializable, IEquatable<HouseRecordNet>
    {
        public int price;
        public ulong ownerClientId; // 0 = none
        public bool forRent;        // ✅ ปล่อยเช่า/ไม่ปล่อย

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

    private void Awake()
    {
        Houses = new NetworkList<HouseRecordNet>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            Houses.Clear();
            for (int i = 0; i < initialPrices.Length; i++)
                Houses.Add(new HouseRecordNet { price = initialPrices[i], ownerClientId = ulong.MaxValue, forRent = false });

            InvokeRepeating(nameof(ServerTickUpdatePricesAndRent), 3f, updateInterval);
        }
    }

    private void ServerTickUpdatePricesAndRent()
    {
        if (!IsServer) return;

        // อ่านตัวคูณอสังหาฯ จาก Event (ไม่มี Event = 1)
        float eventMul = 1f;
        if (EventManagerNet.Instance != null)
            eventMul = EventManagerNet.Instance.GetRealEstateMultiplier();

        for (int i = 0; i < Houses.Count; i++)
        {
            var h = Houses[i];

            // อัปเดตราคาแบบเดิม
            int delta = UnityEngine.Random.Range(-tickK, tickK + 1) * 1_000;
            int basePrice = Mathf.Clamp(h.price + delta, minPrice, maxPrice);

            // นำ Event มาคูณ
            h.price = Mathf.Clamp(Mathf.RoundToInt(basePrice * eventMul), minPrice, maxPrice);

            // จ่ายค่าเช่า
            if (h.ownerClientId != ulong.MaxValue && h.forRent &&
                NetworkManager.Singleton.ConnectedClients.TryGetValue(h.ownerClientId, out var cc) &&
                cc.PlayerObject)
            {
                var inv = cc.PlayerObject.GetComponent<InventoryManager>();
                if (inv) inv.cash.Value += h.price / 100f; // 1% ต่อ tick
            }

            Houses[i] = h; // sync
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void BuyHouseServerRpc(int index, ServerRpcParams rpc = default)
    {
        if (!IsServer || index < 0 || index >= Houses.Count) return;

        var clientId = rpc.Receive.SenderClientId;
        var rec = Houses[index];
        if (rec.ownerClientId != ulong.MaxValue) return; // มีเจ้าของแล้ว //////////

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
        rec.ownerClientId = ulong.MaxValue;      ///////////
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
