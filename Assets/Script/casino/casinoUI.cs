using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.UI;
using Game.Economy;

public class casinoUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI dice1;
    [SerializeField] private TextMeshProUGUI dice2;
    [SerializeField] private TextMeshProUGUI money;
    [SerializeField] private TextMeshProUGUI result;
    [SerializeField] private TMP_InputField amountInput; // optional
    [SerializeField] private Button rollButton;

    [SerializeField] private Button[] PriceButton;
    [SerializeField] private Button[] BetButton;


    [Header("Bet")]
    [SerializeField] private int cost = 10000;
    [SerializeField] private bool hightEven = true;
    [SerializeField] private bool hightOdd = false;
    [SerializeField] private bool lowEven = false;
    [SerializeField] private bool lowOdd = false;

    private InventoryManager inv;

    private void OnEnable()
    {
        SetInteractable(false);
        StartCoroutine(BindLocalInventory());
    }

    private void OnDisable()
    {
        if (inv != null)
        {
            inv.CasinoResultReceived -= OnCasinoResult;
            inv.cash.OnValueChanged -= OnCashChanged;
        }
    }

    private IEnumerator BindLocalInventory()
    {
        // รอให้ Netcode เริ่มทำงาน
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        // หาตัว player ของเครื่องนี้
        var localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        while (localObj == null)
        {
            localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            yield return null;
        }

        inv = localObj.GetComponent<InventoryManager>();
        if (inv == null)
        {
            // fallback เผื่อโปรเจกต์ยังใช้ Instance
            inv = InventoryManager.Instance;
        }

        if (inv == null)
        {
            Debug.LogWarning("[casinoUI] Local InventoryManager not found.");
            yield break;
        }

        // subscribe events
        inv.CasinoResultReceived += OnCasinoResult;
        inv.cash.OnValueChanged += OnCashChanged;

        // updateครั้งแรก
        OnCashChanged(inv.cash.Value, inv.cash.Value);

        SetInteractable(true);
    }

    private void SetInteractable(bool canUse)
    {
        if (rollButton) rollButton.interactable = canUse;
        if (amountInput) amountInput.interactable = canUse;
    }

    private void OnCashChanged(float oldVal, float newVal)
    {
        if (money) money.text = $"{newVal:N0}";
    }

    public void RollDice()
    {
        if (inv == null) return;

        // ถ้ามีช่องกรอกจำนวนเดิมพัน ให้ดึงมาใช้แทน cost คงที่
        int bet = cost;
        if (amountInput && int.TryParse(amountInput.text, out var custom) && custom > 0)
            bet = custom;

        inv.PlaceBetServerRpc(bet, ResolveChoice());
      //  if (result) result.text = "Rolling...";
    }

    private BetChoice ResolveChoice()
    {
        if (hightEven) return BetChoice.HighEven;
        if (hightOdd) return BetChoice.HighOdd;
        if (lowEven) return BetChoice.LowEven;
        return BetChoice.LowOdd;
    }

    private void OnCasinoResult(CasinoResult r)
    {
        if (!string.IsNullOrEmpty(r.message))
        {
            if (result) result.text = r.message;
            return;
        }

        if (dice1) dice1.text = r.dice1.ToString("F0");
        if (dice2) dice2.text = r.dice2.ToString("F0");
        if (result) result.text = r.win ? "เอ็งชนะ" : "เอ็งแพ้";
    }

    // Quick-select bet amount
    public void pick10000() { cost = 10000; if (amountInput) amountInput.text = cost.ToString();}
    public void pick50000() { cost = 50000; if (amountInput) amountInput.text = cost.ToString();}
    public void pick100000() { cost = 100000; if (amountInput) amountInput.text = cost.ToString();}

    public void pickHight_Even() { hightEven = true; hightOdd = lowEven = lowOdd = false;}
    public void pickHight_Odd() { hightOdd = true; hightEven = lowEven = lowOdd = false;}
    public void pickLow_Even() { lowEven = true; hightEven = hightOdd = lowOdd = false;}
    public void pickLow_Odd() { lowOdd = true; hightEven = hightOdd = lowEven = false;}

    private void Start()
    {
        for (int a = 0; a < BetButton.Length; a++)
        {
            int index01 = a; // ต้องเก็บไว้ในตัวแปร local เพื่อไม่ให้ค่า i หลุดตอน callback
            BetButton[a].onClick.AddListener(() => OnButtonClicked(index01));
            BetButton[a].image.color = Color.white; // เริ่มต้นให้เป็นสีขาว
        }
        for (int b = 0; b < PriceButton.Length; b++)
        {
            int index02 = b; // ต้องเก็บไว้ในตัวแปร local เพื่อไม่ให้ค่า i หลุดตอน callback
            PriceButton[b].onClick.AddListener(() => OnButtonClicked01(index02));
            PriceButton[b].image.color = Color.white; // เริ่มต้นให้เป็นสีขาว
        }
    }
    /* private void changColor(int i)
     {
         for (int j = 0; j < allButton.Length; j++)
         {
             if(j == i)
             {
                 ColorBlock colors = allButton[i].colors; 
                 colors = Color.red;
             }
             else 
             {
                // ColorBlock colors = allButton[j].colors; colors.colorMultiplier = 1;
             }
         }


     }*/
    private void OnButtonClicked(int clickedIndex)
    {
        // วนลูปเปลี่ยนสีทุกปุ่ม
        for (int i = 0; i < BetButton.Length; i++)
        {
            if (i == clickedIndex)
                BetButton[i].image.color = Color.gray;  /// ปุ่มที่ถูกกด → สีเทา
            else
                 BetButton[i].image.color = Color.white;   // ปุ่มอื่น → สีขาว
        }
       
    }
    private void OnButtonClicked01(int clickedIndex)
    {
       
        for (int j = 0; j < PriceButton.Length; j++)
        {
            if (j == clickedIndex)
                PriceButton[j].image.color = Color.gray;  // ปุ่มที่ถูกกด → สีเทา
            else
                PriceButton[j].image.color = Color.white;   // ปุ่มอื่น → สีขาว
        }
    }
}
