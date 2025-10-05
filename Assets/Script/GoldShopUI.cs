using UnityEngine;
public class GoldShopUI : MonoBehaviour
{
    public void Buy1Gold()
    {
        InventoryManager.Instance.BuyGoldServerRpc(1, 30000f); // สมมุติราคาทอง 30,000
    }
 

}