using TMPro;
using UnityEngine;
public class CashDisplay : MonoBehaviour
{
    public TMP_Text cashText;
    void Update()
    {
        cashText.text = InventoryManager.Instance.cash.ToString("N0");
    }
}