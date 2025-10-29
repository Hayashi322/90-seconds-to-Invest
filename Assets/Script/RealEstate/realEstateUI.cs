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

    private void SetIndex(int idx)
    {
        currentIndex = idx;
        Refresh();
    }

    public void Buy()
    {
        if (!manager || currentIndex < 0) return;
        manager.BuyHouseServerRpc(currentIndex);
    }

    public void Sell()
    {
        if (!manager || currentIndex < 0) return;
        manager.SellHouseServerRpc(currentIndex);
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

        houseNameText.text = $"House {currentIndex + 1}";
        housePriceText.text = $"Price: {rec.price:N0}";

        string ownerLabel = "None";
        if (rec.ownerClientId != ulong.MaxValue) ////////////////
        {
            ownerLabel = (NetworkManager.Singleton &&
                          NetworkManager.Singleton.LocalClientId == rec.ownerClientId)
                         ? "You"
                         : $"P{rec.ownerClientId}";
        }

        string rentFlag = rec.forRent ? "Yes" : "No";
        ownerHouseText.text = $"Owner : {ownerLabel}  |  For Rent : {rentFlag}";
    }
}
