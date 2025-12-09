using UnityEngine;
using TMPro;

public class CashUI : MonoBehaviour
{
    [Header("Main Cash Text")]
    [SerializeField] private TextMeshProUGUI cashText;

    [Header("Floating Change Text")]
    [SerializeField] private RectTransform floatingParent;     // ที่จะให้ตัวเลขลอยอยู่ (เช่น parent ของ cashText)
    [SerializeField] private FloatingCashText floatingPrefab;  // พรีแฟบตัวเลขลอย

    private void OnEnable()
    {
        // หา InventoryManager ของ local player
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.CashChanged += OnCashChanged;
            // เซ็ตค่าเริ่มต้นครั้งแรก
            UpdateCashText(InventoryManager.Instance.cash.Value);
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.CashChanged -= OnCashChanged;
        }
    }

    private void OnCashChanged(double previous, double current)
    {
        UpdateCashText(current);

        double delta = current - previous;
        if (Mathf.Approximately((float)delta, 0f)) return; // ไม่เปลี่ยนก็ไม่ต้องลอย

        SpawnFloatingText(delta);
    }

    private void UpdateCashText(double value)
    {
        if (!cashText) return;
        cashText.text = $"{value:N0}"; // ใส่ , แบ่งหลักพัน
    }

    private void SpawnFloatingText(double delta)
    {
        if (floatingPrefab == null || floatingParent == null || cashText == null)
            return;

        // สร้างตัวเลขลอยเป็นลูกของ parent เดียวกับตัวเงิน
        var ft = Instantiate(floatingPrefab, floatingParent);

        // ตั้งตำแหน่งเริ่มตรงกับตำแหน่ง cashText
        var ftRect = ft.GetComponent<RectTransform>();
        var cashRect = cashText.GetComponent<RectTransform>();

        ftRect.anchoredPosition = cashRect.anchoredPosition;

        // เซ็ตข้อความ + สี
        bool isGain = delta > 0;
        long intDelta = (long)delta;

        string sign = isGain ? "+" : ""; // ลบมีในตัวเลขอยู่แล้ว
        ft.SetText($"{sign}{intDelta:N0}", isGain);
    }
}
