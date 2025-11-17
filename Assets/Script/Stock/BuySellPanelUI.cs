using System.Collections;
using TMPro;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample.DistributedAuthority;
using UnityEngine;
using UnityEngine.UI;

public class BuySellPanelUI : MonoBehaviour
{
    public TextMeshProUGUI selectedStockText;
    public TMP_InputField volumeInput;
    public TextMeshProUGUI totalPriceText;
    public Button buyButton;
    public Button sellButton;

    [SerializeField] private TextMeshProUGUI[] Stock_text;
    [SerializeField] private int[] Stock_Amount;

    //private StockUIManager uiManager;
    private void Start()
    {
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
        volumeInput.onValueChanged.AddListener(_ => UpdateTotal());
    }

    private void Update()
    {
        var mkt = StockMarketManager.Instance;
      // var uiManager = StockUIManager.Instance;
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
        int AA = 999;
        switch(stock)
        {
            case "PTT":
                AA = 0;
                break;
            case "KBANK":
                AA = 1;
                break;
            case "AOT":
                AA = 2;
                break;
            case "BDMS":
                AA = 3;
                break;
            case "DELTA":
                AA = 4;
                break;
            case "CPNREIT":
                AA = 5;
                break;
            default 
            :AA = 0;
                break;
        }
        if (inv.cash.Value < mkt.GetCurrentPrice(stock)*qty)
            return;
        else
        { Stock_Amount[AA] +=qty;
        if (Stock_Amount[AA] <= 0)
            Stock_Amount[AA] = 0;
        Stock_text[AA].text = $"{Stock_Amount[AA]}";
            Debug.Log("price:"+ mkt.GetCurrentPrice(stock)*qty);
        }
       
        
       
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

        int AA = 999;
        switch (stock)
        {
            case "PTT":
                AA = 0;
                break;
            case "KBANK":
                AA = 1;
                break;
            case "AOT":
                AA = 2;
                break;
            case "BDMS":
                AA = 3;
                break;
            case "DELTA":
                AA = 4;
                break;
            case "CPNREIT":
                AA = 5;
                break;
            default
            :
                AA = 0;
                break;
        }
        if (qty > Stock_Amount[AA])
            return;
        else 
        {
            Stock_Amount[AA] -=qty;
        
        if (Stock_Amount[AA] <= 0)
            Stock_Amount[AA] = 0;
        Stock_text[AA].text = $"{Stock_Amount[AA]}";
        }
        
    }
}
