using UnityEngine;
public class StockMarketUI : MonoBehaviour
{
    public void BuyTechZ()
    {
        InventoryManager.Instance.BuyStock("TECHZ", 1, 100000f);
    }
    public void BuyFina()
    {
        InventoryManager.Instance.BuyStock("FINA", 1, 50000f);
    }
}