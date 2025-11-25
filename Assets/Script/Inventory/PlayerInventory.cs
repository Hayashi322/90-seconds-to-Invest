using TMPro;
using UnityEngine;
using System.Collections;


public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private CanvasGroup inventoryCanvas;
    [SerializeField] private bool isOpen;

    [SerializeField] private TextMeshProUGUI goldAmountValue;

    private InventoryManager inv;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
       
    }

    // Update is called once per frame
    void Update()
    {
        if (isOpen) { openInventory(); }
        else { closeInventory(); }
        if (!inv) return;
        else { goldAmountValue.text = $"{inv.goldAmount.Value:N0}"; }
    }

            
    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }
    private IEnumerator BindWhenReady()
    {
       
        while (InventoryManager.Instance == null) yield return null;
        inv = InventoryManager.Instance;
 
    }

    public void Toggle()
    {
        isOpen = !isOpen;   // สลับค่า true/false
        Debug.Log("isOpen = " + isOpen);
    }
    public void openInventory()
    {
        inventoryCanvas.gameObject.SetActive(true);
    }
    public void closeInventory()
    {
        inventoryCanvas.gameObject.SetActive(false);
    }
}
