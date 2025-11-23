using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class StockMarketManager : NetworkBehaviour
{
    public static StockMarketManager Instance;

    public NetworkList<StockDataNet> networkStocks;

    // ใช้จำตัวที่ผู้เล่นเลือกใน UI
    public string selectedStock;

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
            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"PTT",          // Energy / Oil & Gas
                currentPrice = 31.75f,
                lastPrice = 31.75f,
                volatility = 0.020f    // ±2.0%
            });

            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"KBANK",        // Banking / Finance
                currentPrice = 180.50f,
                lastPrice = 180.50f,
                volatility = 0.018f    // ±1.8%
            });

            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"AOT",          // Tourism / Airports
                currentPrice = 39.50f,
                lastPrice = 39.50f,
                volatility = 0.030f    // ±3.0%
            });

            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"BDMS",         // Healthcare / Hospitals
                currentPrice = 32.00f,
                lastPrice = 32.00f,
                volatility = 0.015f    // ±1.5%
            });

            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"DELTA",        // Technology / Electronics
                currentPrice = 84.00f,
                lastPrice = 84.00f,
                volatility = 0.035f    // ±3.5%
            });

            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"CPNREIT",      // Real Estate / REIT
                currentPrice = 18.00f,
                lastPrice = 18.00f,
                volatility = 0.012f    // ±1.2%
            });


            InvokeRepeating(nameof(UpdateStockPrices), 3f, 5f);
        }
    }

    private void UpdateStockPrices()
    {
        if (!IsServer) return;

        for (int i = 0; i < networkStocks.Count; i++)
        {
            var s = networkStocks[i];
            s.lastPrice = s.currentPrice;

            // ราคาพื้นฐานแบบเดิม
            float fluc = s.currentPrice * s.volatility * Random.Range(-1f, 1f);
            float basePrice = Mathf.Max(1f, s.currentPrice + fluc);

            // 🔥 ตัวคูณตาม Event (ดูจากชื่อหุ้น)
            float eventMul = 1f;
            if (EventManagerNet.Instance != null)
            {
                string symbol = s.stockName.ToString();
                eventMul = EventManagerNet.Instance.GetStockMultiplier(symbol);
            }

            s.currentPrice = Mathf.Max(1f, basePrice * eventMul);

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
