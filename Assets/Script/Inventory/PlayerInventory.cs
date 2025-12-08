using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private CanvasGroup inventoryCanvas;
    [SerializeField] private bool isOpen;

    [Header("Gold Display")]
    [SerializeField] private TextMeshProUGUI goldAmountValue;

    private InventoryManager inv;

    // เรียกครั้งแรกตอนเริ่ม
    private void Start()
    {
        isOpen = false;

        // กันลืมเผื่อเธอลืมเซ็ตใน Inspector
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PlayerInventory] inventoryCanvas ยังไม่ได้เซ็ตใน Inspector");
        }
    }

    // เรียกทุกเฟรม
    private void Update()
    {
        // เปิด/ปิดกระเป๋า
        if (isOpen)
            OpenInventory();
        else
            CloseInventory();

        // ถ้ายังหา InventoryManager ไม่เจอ ก็ไม่ต้องอัปเดตค่า
        if (!inv) return;

        // อัปเดตจำนวนทอง
        if (goldAmountValue != null)
        {
            goldAmountValue.text = $"{inv.goldAmount.Value:N0}";
        }
    }

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        // รอจนกว่า InventoryManager.Instance จะพร้อม
        while (InventoryManager.Instance == null)
            yield return null;

        inv = InventoryManager.Instance;
    }

    // เรียกจากปุ่มใน UI (เชื่อม OnClick → Toggle)
    public void Toggle()
    {
        isOpen = !isOpen;
        Debug.Log("[PlayerInventory] isOpen = " + isOpen);
    }

    private void OpenInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(true);
        }
    }

    private void CloseInventory()
    {
        if (inventoryCanvas != null)
        {
            inventoryCanvas.gameObject.SetActive(false);
        }
    }
}
