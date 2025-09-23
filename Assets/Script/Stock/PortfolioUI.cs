using UnityEngine;
using TMPro;
// using System.Text; // ยังไม่ต้องใช้ถ้ายังไม่แสดงพอร์ต

public class PortfolioUI : MonoBehaviour
{
    public TextMeshProUGUI walletText;
    //public TextMeshProUGUI holdingText;

    void Update()
    {
        walletText.text = $"{InventoryManager.Instance.cash:N0}";

        // ยังไม่แสดงพอร์ตหุ้น
        /*
        StringBuilder sb = new StringBuilder();
        foreach (var entry in InventoryManager.Instance.stockHoldings)
        {
            int qty = entry.Value;
            float price = StockMarketManager.Instance.GetStock(entry.Key)?.currentPrice ?? 0;
            float value = qty * price;
            sb.AppendLine($"{entry.Key}: {qty} หน่วย (~{value:N0} ฿)");
        }

        holdingText.text = sb.ToString();
        */
    }
}
