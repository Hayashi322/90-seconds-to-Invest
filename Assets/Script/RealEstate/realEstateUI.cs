using UnityEngine;
using TMPro;
public class realEstateUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI houseNameText;
    [SerializeField] private TextMeshProUGUI housePriceText;
    [SerializeField] private TextMeshProUGUI ownerHouseText;
    [SerializeField] private int[] housePrice;
    [SerializeField] private bool[] isHouseOwner;

    [SerializeField] private GameObject panel;

    private int Number = -1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void updateState()
    {
        houseNameText.text = "House "+(Number+1);
        housePriceText.text = "Price: " + housePrice[Number];
        if (isHouseOwner[Number] == false)
        {
            ownerHouseText.text = "Owner : None";  //ไม่มีเจ้าของ
        }
        else
        {
            ownerHouseText.text = "Owner : Player"; //ชื่อผู้เล่นที่เป็นเจ้าของ
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
            inv.cash.Value = inv.cash.Value - housePrice[Number];
            isHouseOwner[Number] = true;

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
    public void Check()
    {
        panel.SetActive(true);
    }
}
