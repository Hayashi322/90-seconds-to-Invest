using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StockUI : MonoBehaviour 
{ 
    public TextMeshProUGUI stockNameText; 
    public TextMeshProUGUI priceText; 
    public TextMeshProUGUI changeText; 
    public TMP_InputField quantityInput; 
    public Button buyButton;
    public Button sellButton;
    private StockData data;
    public void Initialize(StockData stock) 
    { 
        data = stock; 
        Refresh(); 
        buyButton.onClick.AddListener(Buy); 
        sellButton.onClick.AddListener(Sell);
    } 

    public void Refresh() 
    { 
        stockNameText.text = data.stockName; priceText.text = $"{data.currentPrice:N2} ฿";
        float change = data.currentPrice - data.lastPrice; changeText.text = $"{(change >= 0 ? "+" : "")}{change:N2}"; 
        changeText.color = change >= 0 ? Color.green : Color.red; 
    } 
    
    void Buy() 
    { 
        int qty = int.TryParse(quantityInput.text, out int result) ? result : 1;
        InventoryManager.Instance.BuyStock(data.stockName, qty, data.currentPrice); 
    } 
    void Sell() 
    { 
        int qty = int.TryParse(quantityInput.text, out int result) ? result : 1;
        InventoryManager.Instance.SellStock(data.stockName, qty, data.currentPrice);
    }
}