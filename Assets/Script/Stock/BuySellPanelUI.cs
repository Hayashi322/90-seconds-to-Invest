using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuySellPanelUI : MonoBehaviour
{
    public TextMeshProUGUI selectedStockText;
    public TMP_InputField volumeInput;
    public TextMeshProUGUI totalPriceText;
    public Button buyButton;
    public Button sellButton;

    void Start()
    {
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
        volumeInput.onValueChanged.AddListener(delegate { UpdateTotal(); });
    }

    void Update()
    {
        string selected = StockMarketManager.Instance.selectedStock;
        if (!string.IsNullOrEmpty(selected))
        {
            selectedStockText.text = selected;
            UpdateTotal();
        }
    }

    void UpdateTotal()
    {
        if (!int.TryParse(volumeInput.text, out int qty)) qty = 0;
        float price = StockMarketManager.Instance.GetStock(StockMarketManager.Instance.selectedStock)?.currentPrice ?? 0f;
        float total = qty * price;
        totalPriceText.text = $"{total:N2}";
    }

    void OnBuy()
    {
        string stock = StockMarketManager.Instance.selectedStock;
        if (string.IsNullOrEmpty(stock)) return;

        int qty = int.TryParse(volumeInput.text, out int v) ? v : 0;
        float price = StockMarketManager.Instance.GetStock(stock).currentPrice;

        InventoryManager.Instance.BuyStock(stock, qty, price);
    }

    void OnSell()
    {
        string stock = StockMarketManager.Instance.selectedStock;
        if (string.IsNullOrEmpty(stock)) return;

        int qty = int.TryParse(volumeInput.text, out int v) ? v : 0;
        float price = StockMarketManager.Instance.GetStock(stock).currentPrice;

        InventoryManager.Instance.SellStock(stock, qty, price);
    }

}
