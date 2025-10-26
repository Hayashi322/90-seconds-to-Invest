using System.Collections;
using TMPro;
using UnityEngine;

public class GoldShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI goldSellText;
    [SerializeField] private TextMeshProUGUI goldBuyText;
    [SerializeField] private TextMeshProUGUI goldAmountText;
    [SerializeField] private TextMeshProUGUI cashText;

    private InventoryManager inv;
    private GoldShopManager shop;

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
        // รอให้มี Instance ต่าง ๆ (กรณี spawn ช้า)
        while (GoldShopManager.Instance == null) yield return null;
        shop = GoldShopManager.Instance;

        while (InventoryManager.Instance == null) yield return null;
        inv = InventoryManager.Instance;

        // subscribe การเปลี่ยนแปลง
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

    private void OnPriceChanged(int _, int __) => RefreshPrices();
    private void OnGoldChanged(int _, int __) => RefreshGold();
    private void OnCashChanged(float _, float __) => RefreshCash();

    private void RefreshAll()
    {
        RefreshPrices();
        RefreshGold();
        RefreshCash();
    }

    private void RefreshPrices()
    {
        if (!shop) return;
        if (goldBuyText) goldBuyText.text = $"Buy:  {shop.BuyGoldPrice.Value:N0}";
        if (goldSellText) goldSellText.text = $"Sell: {shop.SellGoldPrice.Value:N0}";
    }

    private void RefreshGold()
    {
        if (!inv) return;
        if (goldAmountText) goldAmountText.text = $"Gold Amount: {inv.goldAmount.Value:N0}";
    }

    private void RefreshCash()
    {
        if (!inv) return;
        if (cashText) cashText.text = $"{inv.cash.Value:N0}";
    }

    // ===== ปุ่มกด =====
    public void Buy1Gold()
    {
        if (inv == null || shop == null) return;
        inv.BuyGoldServerRpc(1);        // ⬅️ เปลี่ยนมาเรียกเวอร์ชันไม่ส่งราคา
    }

    public void Sell1Gold()
    {
        if (inv == null || shop == null) return;
        inv.SellGoldServerRpc(1);       // ⬅️ เปลี่ยนมาเรียกเวอร์ชันไม่ส่งราคา
    }
}
