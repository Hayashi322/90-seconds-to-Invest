using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RealEstateUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RealEstateManager manager;
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private TextMeshProUGUI housePriceText;
    [SerializeField] private TextMeshProUGUI ownerHouseText;

    [Header("Auto Refresh")]
    [SerializeField] private float uiRefreshEvery = 0.5f;

    private int currentIndex = -1;

    [Header("กระเป๋าผู้เล่น")]
    [SerializeField] private TextMeshProUGUI realEstate_Text;
    [SerializeField] private TextMeshProUGUI realEstatePrice_Text;
    [SerializeField] private bool IsOwnerHouse = false;
    [SerializeField] private bool IsEnoghtMoney;
    [SerializeField] private int B = 999;

    private void Start()
    {
        if (!manager) manager = FindObjectOfType<RealEstateManager>();

        InvokeRepeating(nameof(Refresh), 0.2f, uiRefreshEvery);

        if (manager && manager.Houses != null)
            manager.Houses.OnListChanged += _ => Refresh();
    }

    private void OnDestroy()
    {
        if (manager && manager.Houses != null)
            manager.Houses.OnListChanged -= _ => Refresh();
    }

    public void House1() => SetIndex(0);
    public void House2() => SetIndex(1);
    public void House3() => SetIndex(2);
    public void House4() => SetIndex(3);

    private void Update()
    {
        if (manager == null || manager.Houses == null) return;
        if (currentIndex < 0 || currentIndex >= manager.Houses.Count) return;

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        var rec = manager.Houses[currentIndex];

        IsEnoghtMoney = inv.cash.Value >= rec.price;
    }

    private void SetIndex(int idx)
    {
        currentIndex = idx;
        Refresh();
    }

    public void Buy()
    {
        if (!manager || currentIndex < 0) return;

        var rec = manager.Houses[currentIndex];

        manager.BuyHouseServerRpc(currentIndex);

        if (IsOwnerHouse) return;                // ซื้อบ้านอื่นทับบ้านเดิมไม่ให้ทำซ้ำ

        if (rec.ownerClientId != ulong.MaxValue) // มีเจ้าของแล้ว
            return;

        if (!IsEnoghtMoney) return;

        RealEstateinventoryUI(rec.price, currentIndex);
        IsOwnerHouse = true;
        B = currentIndex;
    }

    public void Sell()
    {
        if (!manager || currentIndex < 0) return;

        manager.SellHouseServerRpc(currentIndex);

        if (B != currentIndex) return;

        if (!IsOwnerHouse) return;

        realEstate_Text.text = "ไม่ได้ซื้อไว้";
        realEstatePrice_Text.text = "";
        IsOwnerHouse = false;
        B = 999;
    }

    // ✅ ปุ่มปล่อยเช่า/ยกเลิกปล่อยเช่า
    public void ForRentOn()
    {
        if (!manager || currentIndex < 0) return;
        manager.ForRentServerRpc(currentIndex, true);
    }

    public void ForRentOff()
    {
        if (!manager || currentIndex < 0) return;
        manager.ForRentServerRpc(currentIndex, false);
    }

    private void Refresh()
    {
        if (!houseNameText || !housePriceText || !ownerHouseText) return;

        if (!manager || manager.Houses == null || manager.Houses.Count == 0 ||
            currentIndex < 0 || currentIndex >= manager.Houses.Count)
        {
            houseNameText.text = "";
            housePriceText.text = "";
            ownerHouseText.text = "";
            return;
        }

        var rec = manager.Houses[currentIndex];

        // ✅ ชื่อบ้านตามที่ต้องการ
        switch (currentIndex)
        {
            case 0: houseNameText.text = "ตึก"; break;
            case 1: houseNameText.text = "บ้านเดี่ยว"; break;
            case 2: houseNameText.text = "บ้านแฝด"; break;
            case 3: houseNameText.text = "คอนโด"; break;
            default: houseNameText.text = $"House {currentIndex + 1}"; break;
        }

        housePriceText.text = $"ราคา: {rec.price:N0}";

        string ownerLabel = "ไม่มีเจ้าของ";
        if (rec.ownerClientId != ulong.MaxValue)
        {
            ownerLabel = GetOwnerLabel(rec.ownerClientId);
        }

        string rentFlag = rec.forRent ? "ใช่" : "ไม่";
        ownerHouseText.text = $"เจ้าของ : {ownerLabel}  |  ปล่อยเช่า : {rentFlag}";
    }

    /// <summary>
    /// คืนชื่อเจ้าของบ้าน จาก clientId
    /// ใช้ LobbyManager.CachedNames ที่ sync มาก่อนหน้านี้
    /// </summary>
    private string GetOwnerLabel(ulong ownerClientId)
    {
        var nm = NetworkManager.Singleton;
        bool isLocalPlayer = (nm != null && nm.LocalClientId == ownerClientId);

        // ดึงจาก cache ของ LobbyManager (ทุกเครื่องจะมีค่าเดียวกันหลังออกจาก Lobby)
        string name = LobbyManager.GetCachedPlayerName(ownerClientId);

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"P{ownerClientId}";   // fallback ถ้าไม่มีชื่อ
        }

        if (isLocalPlayer)
            name += " (ฉัน)";

        Debug.Log($"[RealEstateUI] ownerClientId={ownerClientId}, label={name}");
        return name;
    }

    public void RealEstateinventoryUI(int number, int house)
    {
        realEstatePrice_Text.text = $"ราคา:{number:N0}";
        switch (house)
        {
            case 0: realEstate_Text.text = "ตึก"; break;
            case 1: realEstate_Text.text = "บ้านเดี่ยว"; break;
            case 2: realEstate_Text.text = "บ้านแฝด"; break;
            case 3: realEstate_Text.text = "คอนโด"; break;
            default: realEstate_Text.text = "ไม่ได้ซื้อไว้"; break;
        }
    }
}
