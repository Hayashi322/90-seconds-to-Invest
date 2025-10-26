using UnityEngine;
using Unity.Netcode;

public class GoldShopManager : NetworkBehaviour
{
    public static GoldShopManager Instance;

    [Header("Initial prices")]
    [SerializeField] private int initialBuy = 50_000;
    [SerializeField] private int initialSell = 48_000;

    [Header("Fluctuation")]
    [SerializeField] private int minDelta = -1_000;
    [SerializeField] private int maxDelta = 10_000;
    [SerializeField] private float updateInterval = 5f; // seconds

    // ✅ ราคาที่ซิงก์ข้ามเครือข่าย (Server เขียน / ทุกคนอ่าน)
    public NetworkVariable<int> BuyGoldPrice = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SellGoldPrice = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (BuyGoldPrice.Value == 0) BuyGoldPrice.Value = initialBuy;
            if (SellGoldPrice.Value == 0) SellGoldPrice.Value = initialSell;

            InvokeRepeating(nameof(ServerUpdatePrices), 3f, updateInterval);
        }
    }

    private void OnDestroy()
    {
        if (IsServer) CancelInvoke(nameof(ServerUpdatePrices));
    }

    // รันเฉพาะ Server เท่านั้น
    private void ServerUpdatePrices()
    {
        int delta = Random.Range(minDelta, maxDelta + 1);
        int newBuy = Mathf.Max(1_000, BuyGoldPrice.Value + delta);
        int newSell = Mathf.Max(0, newBuy - 1_000); // กำหนดให้ขายถูกกว่าซื้อเสมอ

        BuyGoldPrice.Value = newBuy;
        SellGoldPrice.Value = newSell;
    }
}
