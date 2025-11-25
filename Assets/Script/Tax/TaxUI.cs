using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaxUI : MonoBehaviour
{
    [Header("ข้อความแสดงผล")]
    [SerializeField] private TextMeshProUGUI phaseHintText; // ข้อความสถานะเฟส
    [SerializeField] private TextMeshProUGUI taxText;       // ยอดภาษี / ชำระแล้ว
    [SerializeField] private TextMeshProUGUI baseText;      // รายได้สุทธิ
    [SerializeField] private TextMeshProUGUI rateText;      // อัตราภาษีที่แท้จริง

    [Header("ปุ่มควบคุม")]
    [SerializeField] private Button payButton;              // ปุ่มจ่ายภาษี
    [SerializeField] private Button closeButton;            // ปุ่มปิดหน้าต่าง

    private TaxManager tax;
    private InventoryManager inv;

    // ถูกเปิดแบบ “โดนสรรพากรบังคับ” หรือเปล่า
    private bool forceMode = false;

    private void Awake()
    {
        if (payButton)
            payButton.onClick.AddListener(OnPay);

        if (closeButton)
            closeButton.onClick.AddListener(OnClickClose);
    }

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private IEnumerator BindWhenReady()
    {
        // รอให้ InventoryManager พร้อม
        while (InventoryManager.Instance == null)
            yield return null;
        inv = InventoryManager.Instance;

        // รอให้ TaxManager พร้อม
        while (TaxManager.Instance == null)
            yield return null;
        tax = TaxManager.Instance;

        // subscribe network vars
        tax.unpaidTax.OnValueChanged += OnAnyChanged;
        tax.taxableBase.OnValueChanged += OnAnyChanged;
        tax.effectiveRate.OnValueChanged += OnAnyChanged;
        inv.cash.OnValueChanged += OnAnyChanged;

        Refresh();
    }

    private void Unsubscribe()
    {
        if (tax != null)
        {
            tax.unpaidTax.OnValueChanged -= OnAnyChanged;
            tax.taxableBase.OnValueChanged -= OnAnyChanged;
            tax.effectiveRate.OnValueChanged -= OnAnyChanged;
        }

        if (inv != null)
        {
            inv.cash.OnValueChanged -= OnAnyChanged;
        }
    }

    private void OnAnyChanged(double _, double __)
    {
        Refresh();
    }

    private void Refresh()
    {
        int phase = (Timer.Instance ? Timer.Instance.Phase : 0);
        bool canPay = (phase == 3);

        // 🔹 แสดงข้อความเฟส
        if (phaseHintText)
        {
            if (canPay)
            {
                phaseHintText.text = "ขณะนี้สามารถชำระภาษีได้ (เฟส 3)";
                phaseHintText.color = new Color(0.20f, 0.80f, 0.30f);
            }
            else
            {
                phaseHintText.text =
                    $"สามารถชำระภาษีได้เฉพาะเฟส 3 เท่านั้น (ขณะนี้อยู่เฟส {Mathf.Max(phase, 1)})";
                phaseHintText.color = new Color(1.00f, 0.80f, 0.25f);
            }
        }

        if (tax == null || inv == null) return;

        double due = tax.unpaidTax.Value;

        // 🔹 แสดงสถานะภาษี
        if (due <= 0f)
        {
            if (taxText) taxText.text = "ภาษี: ชำระแล้ว";
            if (payButton) payButton.interactable = false;
        }
        else
        {
            if (taxText) taxText.text = $"ยอดภาษีที่ต้องชำระ: {due:N0} ฿";
            if (payButton) payButton.interactable = (canPay && inv.cash.Value >= due);
        }

        // 🔹 แสดงรายได้สุทธิและอัตราภาษี
        if (baseText) baseText.text = $"รายได้สุทธิ: {tax.taxableBase.Value:N0} ฿";
        if (rateText) rateText.text = $"จ่ายภาษี: {tax.effectiveRate.Value:P0}";
    }

    // =========================================================
    //  ปุ่ม / การกระทำ
    // =========================================================

    private void OnPay()
    {
        if (tax == null) return;

        // ป้องกันกดผิดเฟส
        if (Timer.Instance == null || Timer.Instance.Phase != 3)
        {
            Debug.Log("สามารถชำระภาษีได้เฉพาะในเฟส 3 เท่านั้น");
            return;
        }

        tax.PayTaxServerRpc(); // Server จะตัดเงินและอัปเดตสถานะให้เอง
    }

    // ปุ่มกากบาท
    private void OnClickClose()
    {
        // ถ้าโดนบังคับ (จาก Event สรรพากร) และยังมีหนี้ภาษี → ห้ามปิด
        if (forceMode &&
            TaxManager.Instance != null &&
            TaxManager.Instance.unpaidTax.Value > 0)
        {
            Debug.Log("[TaxUI] คุณต้องจ่ายภาษีก่อนถึงจะปิดหน้าต่างนี้ได้");
            return;
        }

        forceMode = false;
        gameObject.SetActive(false);
    }

    // เปิดแบบปกติ (ผู้เล่นกดจากเมนู)
    public void OpenNormal()
    {
        forceMode = false;
        gameObject.SetActive(true);
    }

    // เปิดแบบบังคับ (เรียกจาก EventManagerNet → Event “กรมสรรพากรเข้าตรวจสอบ!”)
    public void OpenForceMode()
    {
        forceMode = true;
        gameObject.SetActive(true);
    }

    // ปุ่มเสริม — สำหรับคำนวณภาษีใหม่ (ถ้าต้องการเรียกจากปุ่ม Dev)
    public void CalculateThisPhase()
    {
        if (tax == null) return;
        tax.CalculateTaxThisPhaseServerRpc();
    }
}
