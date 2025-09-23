using UnityEngine;
using System.Collections.Generic;

public class StockMarketManager : MonoBehaviour
{
    public static StockMarketManager Instance;
    public List<StockData> stocks = new List<StockData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InvokeRepeating(nameof(UpdateStockPrices), 3f, 5f);
    }

    void UpdateStockPrices()
    {
        foreach (var stock in stocks)
        {
            stock.lastPrice = stock.currentPrice;
            float fluctuation = stock.currentPrice * stock.volatility * Random.Range(-1f, 1f);
            stock.currentPrice = Mathf.Max(1f, stock.currentPrice + fluctuation);
        }
    }

    public StockData GetStock(string name)
    {
        return stocks.Find(s => s.stockName == name);
    }
}
