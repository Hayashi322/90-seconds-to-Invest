using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;

public class StockMarketUI : MonoBehaviour
{
    public StockMarketManager market;
    public GameObject stockRowPrefab;
    public Transform content;
    public StockInfoPanelUI stockInfoPanel; // ✅ เพิ่มตัวแปรเชื่อมกล่องข้อมูลหุ้น

    private readonly List<StockUI> stockUIs = new List<StockUI>();
    private bool subscribedMarket;
    private bool subscribedHoldings;

    void Start()
    {
        if (!market) market = StockMarketManager.Instance;
        TryBuildRows();

        // subscribe การเปลี่ยนแปลงราคาหุ้น
        if (market && !subscribedMarket)
        {
            market.networkStocks.OnListChanged += OnStocksChanged;
            subscribedMarket = true;
        }

        // subscribe การเปลี่ยนแปลงจำนวนหุ้นที่ถือ
        StartCoroutine(WaitAndSubscribeHoldings());

        // refresh ทุก 5 วิ กันพลาด
        InvokeRepeating(nameof(RefreshAll), 1f, 5f);
    }

    IEnumerator WaitAndSubscribeHoldings()
    {
        while (InventoryManager.Instance == null)
            yield return null;

        if (!subscribedHoldings && InventoryManager.Instance.stockHoldings != null)
        {
            InventoryManager.Instance.stockHoldings.OnListChanged += OnHoldingsChanged;
            subscribedHoldings = true;
        }

        RefreshAll();
    }

    void OnDestroy()
    {
        if (market && subscribedMarket)
            market.networkStocks.OnListChanged -= OnStocksChanged;

        if (subscribedHoldings && InventoryManager.Instance != null &&
            InventoryManager.Instance.stockHoldings != null)
        {
            InventoryManager.Instance.stockHoldings.OnListChanged -= OnHoldingsChanged;
        }
    }

    private void OnStocksChanged(NetworkListEvent<StockDataNet> change)
    {
        RebuildRows();
    }

    private void OnHoldingsChanged(NetworkListEvent<HoldingNet> change)
    {
        RefreshAll();
    }

    private void TryBuildRows()
    {
        if (!market) return;
        if (market.networkStocks == null || market.networkStocks.Count == 0) return;
        RebuildRows();
    }

    private void RebuildRows()
    {
        foreach (var ui in stockUIs) if (ui) Destroy(ui.gameObject);
        stockUIs.Clear();

        for (int i = 0; i < market.networkStocks.Count; i++)
        {
            GameObject go = Instantiate(stockRowPrefab, content);
            var ui = go.GetComponent<StockUI>();
            ui.Initialize(i, this);
            stockUIs.Add(ui);
        }
        RefreshAll();
    }

    void RefreshAll()
    {
        foreach (var ui in stockUIs) ui.Refresh();
    }

    // ✅ เพิ่มส่วนนี้ — แสดงข้อมูลหุ้นที่เลือก
    public void OnStockSelected(int index)
    {
        if (!market) return;
        if (index < 0 || index >= market.networkStocks.Count) return;

        var data = market.networkStocks[index];
        market.selectedStock = data.stockName.ToString();

        // ✅ แสดงข้อมูลใน panel ด้านขวา (ภาษาไทย)
        if (stockInfoPanel)
            stockInfoPanel.ShowInfo(market.selectedStock);
    }
}
