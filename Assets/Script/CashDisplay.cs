using UnityEngine;
using TMPro;

public class CashDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cashText;

    void Update()
    {
        var inv = InventoryManager.Instance;
        if (inv)
            cashText.text = $"{inv.cash.Value:N2} บาท";   // ✅ ใช้ .Value
    }
}
