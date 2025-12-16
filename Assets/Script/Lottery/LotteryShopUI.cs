using System.Collections;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LotteryShopUI : MonoBehaviour
{
    [System.Serializable]
    public class TicketSlotUI
    {
        public Button selectButton;            // ปุ่มกดเพื่อ "เลือก" ใบนี้
        public TextMeshProUGUI numberText;     // แสดงเลข 6 หลัก
        public Image backgroundImage;          // ใช้ไฮไลต์ตอนโดนเลือก (ถ้าไม่เซ็ตจะใช้ Image ของปุ่ม)
    }

    [Header("Ticket Slots (12 ช่อง)")]
    [SerializeField] private TicketSlotUI[] ticketSlots;   // ต้องใส่ 12 element ใน Inspector

    [Header("Info UI")]
    [SerializeField] private TextMeshProUGUI priceText;    // แสดง "หวย รวย\nใบละ 120"

    [Header("Buy Button")]
    [SerializeField] private Button buyButton;             // ปุ่ม "ซื้อ"
    [SerializeField] private Image buyButtonImage;         // รูปบนปุ่มซื้อ
    [SerializeField] private Sprite buyDisabledSprite;     // รูปตอนกดไม่ได้
    [SerializeField] private Sprite buyEnabledSprite;      // รูปตอนกดได้

    private InventoryManager inv;
    private PlayerLotteryState lottery;
    private LotteryManager shop;

    // index ของใบที่เลือกอยู่ตอนนี้ (-1 = ยังไม่ได้เลือก)
    private int selectedIndex = -1;

    [SerializeField] private TextMeshProUGUI InvTicketNumber;
    [SerializeField] private CanvasGroup CanvasGroup;

    private void OnEnable()
    {
        StartCoroutine(BindLocalAndInitRoutine());
    }

    private void OnDisable()
    {
        if (shop != null)
            shop.Slots.OnListChanged -= OnSlotsChanged;

        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnClickBuy);
    }

    private void Update()
    {
        if (lottery != null && InvTicketNumber != null)
        {
            if (lottery.HasTicket.Value)
                InvTicketNumber.text = $"{lottery.TicketNumber.Value:000000}";
            else
                InvTicketNumber.text = "------";
        }
    }

    private IEnumerator BindLocalAndInitRoutine()
    {
        selectedIndex = -1;
        if (priceText) priceText.text = "";

        // ---------- เช็ค ticketSlots ----------
        if (ticketSlots == null || ticketSlots.Length == 0)
        {
            Debug.LogError("[LotteryUI] ticketSlots ยังไม่ถูกเซ็ตใน Inspector");
            yield break;
        }

        // ---------- รอ Netcode ----------
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        // ---------- รอ LotteryManager.Instance ให้ขึ้น ----------
        shop = LotteryManager.Instance;
        int safetyFrame = 0;
        while (shop == null)
        {
            safetyFrame++;
            if (safetyFrame % 60 == 0)
            {
                Debug.LogWarning("[LotteryUI] ยังไม่เจอ LotteryManager.Instance กำลังรอ...");
            }

            shop = LotteryManager.Instance;
            if (shop != null) break;

            yield return null;
        }

        // ตอนนี้ shop ไม่ null แล้ว
        Debug.Log("[LotteryUI] Bind กับ LotteryManager สำเร็จ");

        var nm = NetworkManager.Singleton;
        var localObj = nm.SpawnManager.GetLocalPlayerObject();
        while (localObj == null)
        {
            localObj = nm.SpawnManager.GetLocalPlayerObject();
            yield return null;
        }

        inv = localObj.GetComponent<InventoryManager>();
        lottery = localObj.GetComponent<PlayerLotteryState>();

        if (inv == null)
        {
            Debug.LogError("[LotteryUI] InventoryManager not found on local player");
            yield break;
        }
        if (lottery == null)
        {
            Debug.LogError("[LotteryUI] PlayerLotteryState not found on local player");
            yield break;
        }

        // ---------- ตั้งข้อความราคา ----------
        if (priceText)
            priceText.text = $"ลอตเตอรี่\nใบละ {shop.TicketPrice:N0}";

        // ---------- bind event ----------
        shop.Slots.OnListChanged += OnSlotsChanged;

        // ปุ่มเลือกแต่ละช่อง
        for (int i = 0; i < ticketSlots.Length; i++)
        {
            var ui = ticketSlots[i];
            if (ui == null)
            {
                Debug.LogError($"[LotteryUI] ticketSlots[{i}] ยังไม่ถูกเซ็ต");
                continue;
            }

            if (ui.selectButton == null)
            {
                Debug.LogError($"[LotteryUI] ticketSlots[{i}].selectButton ยังไม่ถูกเซ็ต");
                continue;
            }

            int index = i;
            ui.selectButton.onClick.RemoveAllListeners();
            ui.selectButton.onClick.AddListener(() => OnClickSelect(index));
        }

        // ปุ่มซื้อ
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnClickBuy);

            // ถ้าไม่ได้ลาก buyButtonImage มา ให้ดึงจากตัวปุ่มเอง
            if (buyButtonImage == null)
                buyButtonImage = buyButton.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("[LotteryUI] buyButton ยังไม่ถูกเซ็ตใน Inspector");
        }

        RefreshSlots();
        RefreshBuyButton();
    }

    private void OnSlotsChanged(NetworkListEvent<LotterySlotNet> _)
    {
        if (shop == null) return;

        if (selectedIndex >= shop.Slots.Count)
            selectedIndex = -1;

        RefreshSlots();
        RefreshBuyButton();
    }

    // =========================
    // Refresh UI
    // =========================
    private void RefreshSlots()
    {
        if (shop == null || ticketSlots == null) return;

        int slotCount = shop.Slots.Count;

        for (int i = 0; i < ticketSlots.Length; i++)
        {
            var ui = ticketSlots[i];
            if (ui == null) continue;

            bool hasTicketInThisIndex = i < slotCount;

            if (!hasTicketInThisIndex)
            {
                // ไม่มีหวยใบนี้แล้ว → ซ่อนปุ่ม
                if (ui.numberText) ui.numberText.text = "";
                if (ui.selectButton) ui.selectButton.gameObject.SetActive(false);
                continue;
            }

            var slot = shop.Slots[i];

            if (ui.numberText)
                ui.numberText.text = slot.ticketNumber.ToString("000000");

            if (ui.selectButton)
            {
                ui.selectButton.gameObject.SetActive(true);
                ui.selectButton.interactable = !lottery.HasTicket.Value;
            }

            UpdateSlotHighlight(i, ui);
        }
    }

    private void UpdateSlotHighlight(int index, TicketSlotUI ui)
    {
        var img = ui.backgroundImage;
        if (!img && ui.selectButton)
            img = ui.selectButton.GetComponent<Image>();

        if (!img) return;

        img.color = (index == selectedIndex) ? Color.yellow : Color.white;
    }

    private void RefreshBuyButton()
    {
        if (!buyButton) return;

        bool canBuy = !lottery.HasTicket.Value
                      && selectedIndex >= 0
                      && shop != null
                      && selectedIndex < shop.Slots.Count
                      && inv != null
                      && inv.cash.Value >= shop.TicketPrice;

        buyButton.interactable = canBuy;

        if (buyButtonImage != null)
        {
            if (canBuy && buyEnabledSprite != null)
                buyButtonImage.sprite = buyEnabledSprite;
            else if (!canBuy && buyDisabledSprite != null)
                buyButtonImage.sprite = buyDisabledSprite;
        }
    }

    // =========================
    // Interaction
    // =========================
    private void OnClickSelect(int index)
    {
        if (shop == null || lottery == null) return;
        if (lottery.HasTicket.Value) return;
        if (index < 0 || index >= shop.Slots.Count) return;

        selectedIndex = index;
        RefreshSlots();
        RefreshBuyButton();
    }

    private void OnClickBuy()
    {
        if (shop == null || lottery == null || inv == null) return;
        if (lottery.HasTicket.Value) return;
        if (selectedIndex < 0 || selectedIndex >= shop.Slots.Count) return;
        if (inv.cash.Value < shop.TicketPrice) return;

        lottery.BuyTicketAtIndexServerRpc(selectedIndex);
        selectedIndex = -1;
        RefreshBuyButton();
    }

    public void OnClickClose()
    {
#if UNITY_2023_1_OR_NEWER
        var oc = FindFirstObjectByType<OpenCanvas>(FindObjectsInactive.Include);
#else
        var oc = FindObjectOfType<OpenCanvas>(true);
#endif
        if (oc != null)
            oc.closeCanvas();
        else
            gameObject.SetActive(false);
    }

    public void AddInventory()
    {
        CanvasGroup.alpha = 1;
    }
}
