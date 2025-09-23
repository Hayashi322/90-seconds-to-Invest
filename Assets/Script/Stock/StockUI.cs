using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stockNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI changeText;
    [SerializeField] private Button selectButton;

    private StockData data;
    private StockMarketUI marketUI;

    public void Initialize(StockData stock, StockMarketUI parentUI)
    {
        data = stock;
        marketUI = parentUI;

        stockNameText.text = data.stockName;
        selectButton.onClick.AddListener(OnSelect);
        Refresh();
    }

    public void Refresh()
    {
        priceText.text = $"{data.currentPrice:N2}";
        float change = data.currentPrice - data.lastPrice;
        changeText.text = $"{(change >= 0 ? "+" : "")}{change:N2}";
        changeText.color = change >= 0 ? Color.green : Color.red;
    }

    private void OnSelect()
    {
        marketUI.OnStockSelected(data);
    }
}
