using Unity.Netcode;
using UnityEngine;

public class GoldShopManager : NetworkBehaviour
{
    public static GoldShopManager Instance;

    [Header("Initial prices")]
    [SerializeField] private int initialBuy = 50_000;
    [SerializeField] private int initialSell = 48_000;

    [Header("Fluctuation")]
    [SerializeField] private int minDelta = -10;
    [SerializeField] private int maxDelta = 50;
    [SerializeField] private float updateInterval = 10f; // seconds

    // ✅ ราคาที่ซิงก์ข้ามเครือข่าย (Server เขียน / ทุกคนอ่าน)
    public NetworkVariable<int> BuyGoldPrice =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SellGoldPrice =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> GoldChangePrice =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ราคา "ฐาน" ฝั่ง Server
    private int baseBuy;

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
            if (BuyGoldPrice.Value == 0)
            {
                baseBuy = initialBuy;
                BuyGoldPrice.Value = baseBuy;
            }
            else
            {
                baseBuy = BuyGoldPrice.Value;
            }

            if (SellGoldPrice.Value == 0)
                SellGoldPrice.Value = initialSell;

            InvokeRepeating(nameof(ServerUpdatePrices), 3f, updateInterval);
        }
    }

    private void OnDestroy()
    {
        if (IsServer) CancelInvoke(nameof(ServerUpdatePrices));
        if (Instance == this) Instance = null;
    }

    // รันเฉพาะ Server เท่านั้น
    private void ServerUpdatePrices()
    {
        // 1) อัปเดต base price ตาม delta
        int delta = Random.Range(minDelta, maxDelta + 1) * 400;
        baseBuy = Mathf.Clamp(baseBuy + delta, 10_000, 70_000);

        // 2) ตัวคูณจาก Event
        float eventMul = 1f;
        if (EventManagerNet.Instance != null)
            eventMul = EventManagerNet.Instance.GetGoldMultiplier();

        // 3) ราคาจริง = base * eventMultiplier
        int newBuy = Mathf.Clamp(Mathf.RoundToInt(baseBuy * eventMul), 10_000, 70_000);
        int newSell = Mathf.Max(0, newBuy - 100);

        GoldChangePrice.Value = newBuy - BuyGoldPrice.Value;

        BuyGoldPrice.Value = newBuy;
        SellGoldPrice.Value = newSell;
    }
}
