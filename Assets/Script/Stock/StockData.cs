using UnityEngine;

[System.Serializable]
public class StockData
{
    public string stockName;    // เช่น "TTP"
    public float currentPrice;  // ราคาปัจจุบัน
    public float lastPrice;     // ราคาก่อนหน้า
    public float volatility;    // ความผันผวน เช่น 0.03f = ±3%
}
