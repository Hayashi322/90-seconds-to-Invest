using UnityEngine;
using System.Collections.Generic;

public class StockMarketUI : MonoBehaviour
{
    public StockMarketManager market;
    public GameObject stockRowPrefab;
    public Transform content;

    private List<StockUI> stockUIs = new List<StockUI>();

    void Start()
    {
        foreach (var stock in market.stocks)
        {
            GameObject go = Instantiate(stockRowPrefab, content);
            var ui = go.GetComponent<StockUI>();
            ui.Initialize(stock, this);
            stockUIs.Add(ui);
        }

        InvokeRepeating(nameof(RefreshAll), 1f, 5f);
    }

    void RefreshAll()
    {
        foreach (var ui in stockUIs)
            ui.Refresh();
    }

    public void OnStockSelected(StockData stock)
    {
        StockMarketManager.Instance.selectedStock = stock.stockName;
    }
}
