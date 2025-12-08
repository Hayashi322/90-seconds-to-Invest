using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86;

public class GoldShopUI : MonoBehaviour
{
    [Header("UI (เดิม)")]
    [SerializeField] private TextMeshProUGUI goldsellValue;   // SellGold_Text
    [SerializeField] private TextMeshProUGUI goldbuyValue;    // BuyGold_Text
    [SerializeField] private TextMeshProUGUI goldAmountValue; // GoldAmount_Text
    [SerializeField] private TextMeshProUGUI goldChangePrice; // GoldChangePrice_Text

    [Header("UI กระเป๋าผู้เล่น")]
    [SerializeField] private TextMeshProUGUI InvgoldAmountValue;
    // (ถ้ามีช่องโชว์เงิน ให้เพิ่ม TextMeshProUGUI cashText; แล้วเติมใน RefreshCash())

    private GoldShopManager shop;      // มี NetworkVariable ราคาทอง
    private InventoryManager inv;      // มี NetworkVariable goldAmount / cash

    [SerializeField] private TMP_InputField GoldInput;
    private int value; //จำนวนทองที่ใส่ในช่อง
    [SerializeField] private TextMeshProUGUI averageTMP;


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
        // รอให้ทั้งสองตัวมี Instance (กรณี spawn ช้า)
        while (GoldShopManager.Instance == null) yield return null;
        shop = GoldShopManager.Instance;

        while (InventoryManager.Instance == null) yield return null;
        inv = InventoryManager.Instance;

        // subscribe การเปลี่ยนแปลง (อัปเดตเฉพาะตอนมีการเปลี่ยน ไม่ต้องวิ่งทุกเฟรม)
        shop.BuyGoldPrice.OnValueChanged += OnPriceChanged;
        shop.SellGoldPrice.OnValueChanged += OnPriceChanged;
        inv.goldAmount.OnValueChanged += OnGoldChanged;
        inv.cash.OnValueChanged += OnCashChanged;

        // อัปเดตครั้งแรก
        RefreshAll();
    }

    private void Unsubscribe()
    {
        if (shop != null)
        {
            shop.BuyGoldPrice.OnValueChanged -= OnPriceChanged;
            shop.SellGoldPrice.OnValueChanged -= OnPriceChanged;
        }
        if (inv != null)
        {
            inv.goldAmount.OnValueChanged -= OnGoldChanged;
            inv.cash.OnValueChanged -= OnCashChanged;
        }
    }

    // ===== handlers =====
    private void OnPriceChanged(int _, int __) => RefreshPrices();
    private void OnGoldChanged(int _, int __) => RefreshGold();
    private void OnCashChanged(double _, double __) => RefreshCash();

    // ===== refreshers =====
    private void RefreshAll()
    {
        RefreshPrices();
        RefreshGold();
        RefreshCash();
    }

    private void RefreshPrices()
    {
        if (!shop) return;
        if (goldbuyValue) goldbuyValue.text = $"{shop.BuyGoldPrice.Value:N0}";
        if (goldsellValue) goldsellValue.text = $"{shop.SellGoldPrice.Value:N0}";
        if(goldChangePrice) goldChangePrice.text = $"ปรับราคา: {shop.GoldChangePrice.Value:N0}";
    }

    private void Update()
    {
        RefreshPrices() ;
        UpdateUI();

    }
 
    private void RefreshGold()
    {
        if (!inv) return;
        if (goldAmountValue)
        { goldAmountValue.text = $"{inv.goldAmount.Value:N0}";/* InvgoldAmountValue.text = $"จำนวนทอง:{inv.goldAmount.Value:N0}"; */}

    }

    private void RefreshCash()
    {
        // ถ้ามีช่องเงิน ให้ใส่ที่นี่ เช่น:
        // if (cashText) cashText.text = $"{inv.cash.Value:N0}";
    }

    // ===== ปุ่มกด (ถ้าผูกปุ่มไว้ที่สคริปต์นี้) =====
    public void BuyGold()
    {
        if (inv == null) return;
        if(value<=0) { return; }
        if (value > 1000) {  return; }
        inv.BuyGoldServerRpc(value);   // server จะอ่านราคาจาก GoldShopManager เอง
    }

    public void SellGold()
    {
        if (inv == null) return;
        if (value<= 0) { return; }
        if(value> inv.goldAmount.Value) { value = inv.goldAmount.Value; }
        inv.SellGoldServerRpc(value);
    }
    public void OnSubmit()
    {
        if (int.TryParse(GoldInput.text, out value))
        {
            Debug.Log("ค่าที่กรอกคือ: " + value);
        }
        else
        {
            Debug.Log("กรุณาใส่เฉพาะตัวเลข");
        }
    }
    public void UpdateUI()
    {
        if (inv.goldAmount.Value <= 0) { averageTMP.text = "ต้นทุนทองเฉลี่ย:0"; }; //อัพเดทข้อความค่าเฉลี่ย
        averageTMP.text = $"ต้นทุนทองเฉลี่ย:{inv.averageGoldPrice.Value}";
     
    }
}
