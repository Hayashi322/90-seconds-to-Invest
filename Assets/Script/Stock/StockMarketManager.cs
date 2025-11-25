using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StockMarketManager : NetworkBehaviour
{
    public static StockMarketManager Instance;

    public NetworkList<StockDataNet> networkStocks;

    // ใช้จำตัวที่ผู้เล่นเลือกใน UI
    public string selectedStock;

    // ราคา "ฐาน" ต่อหุ้น ใช้เฉพาะฝั่ง Server
    private float[] basePrices;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        networkStocks = new NetworkList<StockDataNet>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            networkStocks.Clear();

            // สร้างหุ้นเริ่มต้น
            AddStock("PTT", 31.75f, 0.020f); // Energy / Oil & Gas
            AddStock("KBANK", 180.50f, 0.018f); // Banking / Finance
            AddStock("AOT", 39.50f, 0.030f); // Tourism / Airports
            AddStock("BDMS", 32.00f, 0.015f); // Healthcare / Hospitals
            AddStock("DELTA", 84.00f, 0.035f); // Technology / Electronics
            AddStock("CPNREIT", 18.00f, 0.012f); // Real Estate / REIT

            // สร้าง basePrices ให้ยาวเท่ากับจำนวนหุ้น
            basePrices = new float[networkStocks.Count];
            for (int i = 0; i < networkStocks.Count; i++)
            {
                basePrices[i] = networkStocks[i].currentPrice;
            }

            InvokeRepeating(nameof(UpdateStockPrices), 3f, 5f);
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            CancelInvoke(nameof(UpdateStockPrices));
        }

        if (Instance == this)
            Instance = null;
    }

    private void AddStock(string symbol, float price, float volatility)
    {
        networkStocks.Add(new StockDataNet
        {
            stockName = (FixedString32Bytes)symbol,
            currentPrice = price,
            lastPrice = price,
            volatility = volatility
        });
    }

    private void UpdateStockPrices()
    {
        if (!IsServer) return;
        if (networkStocks.Count == 0) return;

        if (basePrices == null || basePrices.Length != networkStocks.Count)
        {
            basePrices = new float[networkStocks.Count];
            for (int i = 0; i < networkStocks.Count; i++)
                basePrices[i] = networkStocks[i].currentPrice;
        }

        for (int i = 0; i < networkStocks.Count; i++)
        {
            var s = networkStocks[i];

            // 1) อัปเดต base price ตาม volatility
            float baseP = basePrices[i];
            float fluc = baseP * s.volatility * Random.Range(-1f, 1f);
            baseP = Mathf.Max(1f, baseP + fluc);
            basePrices[i] = baseP;

            // 2) ดึงตัวคูณจาก Event ตาม symbol
            float eventMul = 1f;
            if (EventManagerNet.Instance != null)
            {
                string symbol = s.stockName.ToString();
                eventMul = EventManagerNet.Instance.GetStockMultiplier(symbol);
            }

            // 3) อัปเดตราคาโชว์จริง
            s.lastPrice = s.currentPrice;
            s.currentPrice = Mathf.Max(1f, baseP * eventMul);

            networkStocks[i] = s;  // trigger sync
        }
    }

    // ---------- Helpers ให้ UI ใช้ ----------
    public bool TryGetStockByName(string name, out StockDataNet s)
    {
        s = default;
        if (string.IsNullOrEmpty(name) || networkStocks == null) return false;

        var key = (FixedString32Bytes)name;
        for (int i = 0; i < networkStocks.Count; i++)
        {
            if (networkStocks[i].stockName.Equals(key))
            {
                s = networkStocks[i];
                return true;
            }
        }
        return false;
    }

    public float GetCurrentPrice(string name)
    {
        return TryGetStockByName(name, out var s) ? s.currentPrice : 0f;
    }
}
