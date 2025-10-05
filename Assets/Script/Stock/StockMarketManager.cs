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
            // ตัวอย่างหุ้นเริ่มต้น
            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"TTP",
                currentPrice = 100,
                lastPrice = 100,
                volatility = 0.05f
            });
            networkStocks.Add(new StockDataNet
            {
                stockName = (FixedString32Bytes)"JBANK",
                currentPrice = 200,
                lastPrice = 200,
                volatility = 0.04f
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
            float fluc = s.currentPrice * s.volatility * Random.Range(-1f, 1f);
            s.currentPrice = Mathf.Max(1f, s.currentPrice + fluc);
            networkStocks[i] = s;  // ต้องเขียนกลับเพื่อ trigger sync
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
