using UnityEngine;
using TMPro;

public class realEstateUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private TextMeshProUGUI housePriceText;
    [SerializeField] private TextMeshProUGUI ownerHouseText;
    [SerializeField] private int[] housePrice;
    [SerializeField] private bool[] isHouseOwner;
    [SerializeField] private float updateInterval = 10f;

    [SerializeField] private GameObject panel;

    private int Number = -1;

    // ✅ เปิด panel อัตโนมัติเมื่อเข้าหน้าร้านอสังหาฯ
    void Start()
    {
        InvokeRepeating(nameof(UpdatePrices), 3f, updateInterval);
    }

    void Update()
    {
        if (Number < 0)
        {
            houseNameText.text = "";
            housePriceText.text = "";
            ownerHouseText.text = "";
        }
    }

    private void updateState()
    {
        houseNameText.text = "House " + (Number + 1);
        housePriceText.text = "Price: " + housePrice[Number];
        if (isHouseOwner[Number] == false)
        {
            ownerHouseText.text = "Owner : None";
        }
        else
        {
            ownerHouseText.text = "Owner : Player";
        }
    }

    public void house1()
    {
        Number = 0;
        updateState();
    }
    public void house2()
    {
        Number = 1;
        updateState();
    }
    public void house3()
    {
        Number = 2;
        updateState();
    }
    public void house4()
    {
        Number = 3;
        updateState();
    }

    public void Buy()
    {
        if (isHouseOwner[Number] == true) { return; }
        else
        {
            var inv = InventoryManager.Instance;
            if (inv.cash.Value < housePrice[Number])
            { return; }
            else
            {
                inv.cash.Value = inv.cash.Value - housePrice[Number];
                isHouseOwner[Number] = true;
            }
        }

        updateState();
    }

    public void Sell()
    {
        if (isHouseOwner[Number] == false)
        { return; }
        else
        {
            var inv = InventoryManager.Instance;
            inv.cash.Value = inv.cash.Value + housePrice[Number];
            isHouseOwner[Number] = false;
        }

        updateState();
    }

    private void UpdatePrices()
    {
        for (int i = 0; i < housePrice.Length; i++)
        {
            int delta = Random.Range(-50, 50 + 1);
            delta = delta * 1_000;

            Debug.Log(delta);

            int newBuy = Mathf.Max(1_000, housePrice[i] + delta);
            Debug.Log("NewBuy: " + newBuy);

            if (newBuy > 20_000_000)
            {
                newBuy = 20_000_000;
            }
            if (newBuy < 5_000_000)
            {
                newBuy = 5_000_000;
            }

            housePrice[i] = newBuy;
            Debug.Log("House Price:" + i + " " + housePrice[i]);
            updateState();
        }
    }

    public void For_Rent()
    {
        InvokeRepeating(nameof(getForRent), 3f, updateInterval);
    }

    private void getForRent()
    {
        var inv = InventoryManager.Instance;
        for (int i = 0; i < isHouseOwner.Length; i++)
            if (isHouseOwner[i] == true)
            {
                inv.cash.Value = inv.cash.Value + housePrice[i] / 100;
            }
            else { continue; }
    }
}
