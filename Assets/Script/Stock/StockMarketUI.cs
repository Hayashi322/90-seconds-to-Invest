using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;

public class StockMarketUI : MonoBehaviour
{
    public StockMarketManager market;
    public GameObject stockRowPrefab;
    public Transform content;

    private readonly List<StockUI> stockUIs = new List<StockUI>();
    private bool subscribed;

    void Start()
    {
        if (!market) market = StockMarketManager.Instance;
        TryBuildRows();

        // subscribe ครั้งเดียว
        if (market && !subscribed)
        {
            market.networkStocks.OnListChanged += OnStocksChanged;
            subscribed = true;
        }

        InvokeRepeating(nameof(RefreshAll), 1f, 5f);
    }

    void OnDestroy()
    {
        if (market && subscribed)
            market.networkStocks.OnListChanged -= OnStocksChanged;
    }

    // เรียกเมื่อรายการใน NetworkList เปลี่ยน
    private void OnStocksChanged(NetworkListEvent<StockDataNet> change)
    {
        RebuildRows();
    }

    private void TryBuildRows()
    {
        if (!market) return;
        if (market.networkStocks == null || market.networkStocks.Count == 0) return;
        RebuildRows();
    }

    private void RebuildRows()
    {
        // ล้างของเดิม
        foreach (var ui in stockUIs) if (ui) Destroy(ui.gameObject);
        stockUIs.Clear();

        // สร้างใหม่ตามจำนวนหุ้นใน networkStocks
        for (int i = 0; i < market.networkStocks.Count; i++)
        {
            GameObject go = Instantiate(stockRowPrefab, content);
            var ui = go.GetComponent<StockUI>();
            ui.Initialize(i, this);          // ใช้ index
            stockUIs.Add(ui);
        }
        RefreshAll();
    }

    void RefreshAll()
    {
        foreach (var ui in stockUIs) ui.Refresh();
    }

    // เรียกจากปุ่ม Select ของแต่ละแถว
    public void OnStockSelected(int index)
    {
        if (!market) return;
        if (index < 0 || index >= market.networkStocks.Count) return;

        var s = market.networkStocks[index];
        market.selectedStock = s.stockName.ToString();  // 👉 เก็บชื่อไว้ให้ UI อื่นใช้
    }
}
