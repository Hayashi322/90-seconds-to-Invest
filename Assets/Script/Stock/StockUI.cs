using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stockNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI changeText;
    [SerializeField] private Button selectButton;

    private int index;
    private StockMarketUI parentUI;
    private StockMarketManager Market => StockMarketManager.Instance;

    public void Initialize(int idx, StockMarketUI parent)
    {
        index = idx;
        parentUI = parent;
        selectButton.onClick.AddListener(OnSelect);
        Refresh();
    }

    public void Refresh()
    {
        if (!Market || Market.networkStocks == null) return;
        if (index < 0 || index >= Market.networkStocks.Count) return;

        var data = Market.networkStocks[index];
        stockNameText.text = data.stockName.ToString();
        priceText.text = $"{data.currentPrice:N2}";
        float change = data.currentPrice - data.lastPrice;
        changeText.text = $"{(change >= 0 ? "+" : "")}{change:N2}";
        changeText.color = change >= 0 ? Color.green : Color.red;
    }

    private void OnSelect()
    {
        parentUI?.OnStockSelected(index);
    }
}
