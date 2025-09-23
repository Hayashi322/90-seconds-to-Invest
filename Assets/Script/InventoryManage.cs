using System.Collections.Generic;
using UnityEngine;
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public float cash = 10000000f; // เริ่มต้น 10 ล้าน
    public int goldAmount = 0;
    public Dictionary<string, int> stockHoldings = new Dictionary<string, int>();
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void BuyGold(int qty, float price)
    {
        float cost = qty * price;
        if (cash >= cost)
        {
            cash -= cost;
            goldAmount += qty;
            Debug.Log($"ซื้อทอง {qty} ชิ้น | ทองรวม: {goldAmount} | เงินคงเหลือ: {cash}");
        }
    }
    public void BuyStock(string stockName, int qty, float price)
    {
        float total = qty * price;
        if (cash >= total)
        {
            cash -= total;
            if (!stockHoldings.ContainsKey(stockName))
                stockHoldings[stockName] = 0;
            stockHoldings[stockName] += qty;
        }
    }

    public void SellStock(string stockName, int qty, float price)
    {
        if (stockHoldings.ContainsKey(stockName) && stockHoldings[stockName] >= qty)
        {
            cash += qty * price;
            stockHoldings[stockName] -= qty;
        }
    }

    public int GetStockQuantity(string stockName)
    {
        return stockHoldings.ContainsKey(stockName) ? stockHoldings[stockName] : 0;
    }

}