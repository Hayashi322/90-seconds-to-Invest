using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuySellPanelUI : MonoBehaviour
{
    public TextMeshProUGUI selectedStockText;
    public TMP_InputField volumeInput;
    public TextMeshProUGUI totalPriceText;
    public Button buyButton;
    public Button sellButton;

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
        volumeInput.onValueChanged.AddListener(_ => UpdateTotal());
    }

    private void Update()
    {
        var mkt = StockMarketManager.Instance;
        if (mkt != null && !string.IsNullOrEmpty(mkt.selectedStock))
        {
            selectedStockText.text = mkt.selectedStock;
            UpdateTotal();
        }
    }

    private void UpdateTotal()
    {
        if (!int.TryParse(volumeInput.text, out int qty)) qty = 0;
        float price = 0f;
        var mkt = StockMarketManager.Instance;
        if (mkt != null) price = mkt.GetCurrentPrice(mkt.selectedStock);
        float total = qty * price;
        totalPriceText.text = $"{total:N2}";
    }

    private void OnBuy()
    {
        var mkt = StockMarketManager.Instance;
        var inv = InventoryManager.Instance;
        if (mkt == null || inv == null) return;

        string stock = mkt.selectedStock;
        if (string.IsNullOrEmpty(stock)) return;

        int qty = int.TryParse(volumeInput.text, out int v) ? v : 0;
        float price = mkt.GetCurrentPrice(stock);

        inv.BuyStockServerRpc(stock, qty, price); // เรียกที่ Server
    }

    private void OnSell()
    {
        var mkt = StockMarketManager.Instance;
        var inv = InventoryManager.Instance;
        if (mkt == null || inv == null) return;

        string stock = mkt.selectedStock;
        if (string.IsNullOrEmpty(stock)) return;

        int qty = int.TryParse(volumeInput.text, out int v) ? v : 0;
        float price = mkt.GetCurrentPrice(stock);

        inv.SellStockServerRpc(stock, qty, price); // เรียกที่ Server
    }
}
