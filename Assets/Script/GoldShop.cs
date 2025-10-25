using NUnit;
using System.Runtime.CompilerServices;
using UnityEngine;
using TMPro;

public class GoldShop : MonoBehaviour
{
    private int goldPriceRate;
    private int BuyGoldPrice = 50000;
    private int SellGoldPrice= 48000;
    private int goldAmount=0;
   [SerializeField] private TextMeshProUGUI goldsellValue;
    [SerializeField] private TextMeshProUGUI goldbuyValue;
    [SerializeField] private TextMeshProUGUI goldAmountValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       randomGold();
        InvokeRepeating(nameof(UpdateStockPrices), 3f, 5f);
    }

    // Update is called once per frame
    void Update()
    {
        

        //BuyGoldPrice =BuyGoldPrice+ goldPriceRate;
       // SellGoldPrice =SellGoldPrice+ goldPriceRate;
        goldsellValue.text = "Sell: "+SellGoldPrice;
        goldbuyValue.text = "Buy: "+BuyGoldPrice;
        goldAmountValue.text="Gold Amount: "+goldAmount;
    }
    void randomGold()
    {
       goldPriceRate= Random.Range(-1000, 10000);
        BuyGoldPrice = BuyGoldPrice + goldPriceRate;
        SellGoldPrice = (SellGoldPrice + goldPriceRate)-1000;
    }
    public void BuyGold()
    {
        var inv = InventoryManager.Instance;
        inv.cash.Value = inv.cash.Value - BuyGoldPrice;
        goldAmount++;

    }
    public void SellGold() 
    {
        
        if(goldAmount > 0) 
        {
            goldAmount--;
            var inv = InventoryManager.Instance;
            inv.cash.Value =inv.cash.Value+ SellGoldPrice;
        }
        else { return; }
    }
    void UpdateStockPrices()
    {
        for(int i = 0; i < 1; i++) 
        {
            randomGold();
        }
    }
}
