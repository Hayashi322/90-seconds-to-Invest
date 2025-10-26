using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI taxText;   // shows due or message
    [SerializeField] private Button payButton;          // "Pay tax"
    [SerializeField] private Button closeButton;        // optional close

    private TaxManager tax;
    private InventoryManager inv;

    private void Awake()
    {
        if (payButton) payButton.onClick.AddListener(OnPay);
        if (closeButton) closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable() => StartCoroutine(BindWhenReady());
    private void OnDisable() => Unsubscribe();

    private IEnumerator BindWhenReady()
    {
        while (InventoryManager.Instance == null) yield return null;
        inv = InventoryManager.Instance;

        while (TaxManager.Instance == null) yield return null;
        tax = TaxManager.Instance;

        tax.unpaidTax.OnValueChanged += OnUnpaidChanged;
        inv.cash.OnValueChanged += OnCashChanged;

        Refresh();
    }

    private void Unsubscribe()
    {
        if (tax != null) tax.unpaidTax.OnValueChanged -= OnUnpaidChanged;
        if (inv != null) inv.cash.OnValueChanged -= OnCashChanged;
    }

    private void OnUnpaidChanged(float _, float __) => Refresh();
    private void OnCashChanged(float _, float __) => Refresh();

    private void Refresh()
    {
        if (tax == null || inv == null) return;

        float due = tax.unpaidTax.Value;
        bool isPhase2 = (Timer.Instance != null && Timer.Instance.IsPhase2);

        if (due <= 0f)
        {
            if (taxText) taxText.text = "Paid";
            if (payButton) payButton.interactable = false;
        }
        else
        {
            if (taxText) taxText.text = $"Tax Due: {due:N0} ฿";
            if (payButton) payButton.interactable = isPhase2 && (inv.cash.Value >= due);
        }
    }

    private void OnPay()
    {
        if (tax == null) return;
        tax.PayTaxServerRpc();   // ❌ ไม่ต้องเช็ค Timer/Phase อีกต่อไป
    }

    // optional button to compute tax for this phase (client requests server)
    public void CalculateThisPhase()
    {
        if (tax == null) return;
        tax.CalculateTaxServerRpc();
    }
}
