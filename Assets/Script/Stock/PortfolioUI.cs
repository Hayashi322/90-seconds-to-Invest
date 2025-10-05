using TMPro;
using UnityEngine;

public class PortfolioUI : MonoBehaviour
{
    public TextMeshProUGUI walletText;

    private void Update()
    {
        var inv = InventoryManager.Instance;
        if (inv != null)
            walletText.text = $"{inv.cash.Value:N0}";
    }
}
