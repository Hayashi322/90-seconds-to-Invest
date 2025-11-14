using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.UI;
// using Game.Economy; // ถ้าไม่ได้ใช้ให้ลบได้

public class casinoUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI dice1;
    [SerializeField] private TextMeshProUGUI dice2;
    [SerializeField] private TextMeshProUGUI money;
    [SerializeField] private TextMeshProUGUI result;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button rollButton;

    [SerializeField] private Button[] PriceButton;
    [SerializeField] private Button[] BetButton;

    [Header("Enter Message")]
    [SerializeField] private TextMeshProUGUI enterMessageText;   // ← ลาก Text TMP สำหรับโชว์ข้อความเข้า UI
    [SerializeField] private float enterMessageDuration = 3f;    // ระยะเวลาที่แสดงข้อความ (วินาที)

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

        // แสดงข้อความเมื่อเข้าหน้านี้
        if (enterMessageText)
        {
            StopAllCoroutines();                    // กันเคสเปิด/ปิดซ้ำเร็ว ๆ
            StartCoroutine(ShowEnterMessage());
        }
        else
        {
            // ถ้าไม่ได้เซ็ตช่องไว้ ใช้ result แทนชั่วคราว
            if (result) StartCoroutine(ShowTempOn(result));
        }
    }

    private IEnumerator ShowEnterMessage()
    {
        enterMessageText.text = "ตำรวจมาตัวใครตัวมันเด้อ";
        enterMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(enterMessageDuration);

        // เคลียร์ข้อความหลังครบเวลา
        enterMessageText.text = "";
        enterMessageText.gameObject.SetActive(false);
    }

    private IEnumerator ShowTempOn(TextMeshProUGUI target)
    {
        string backup = target.text;
        target.text = "ตำรวจมาตัวใครตัวมันเด้อ";
        yield return new WaitForSeconds(enterMessageDuration);
        target.text = backup;
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
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        var localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        while (localObj == null)
        {
            localObj = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            yield return null;
        }

        inv = localObj.GetComponent<InventoryManager>();
        if (inv == null) inv = InventoryManager.Instance;
        if (inv == null) yield break;

        inv.CasinoResultReceived += OnCasinoResult;
        inv.cash.OnValueChanged += OnCashChanged;

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

        int bet = cost;
        if (amountInput && int.TryParse(amountInput.text, out var custom) && custom > 0)
            bet = custom;

        inv.PlaceBetServerRpc(bet, ResolveChoice());
        // if (result) result.text = "Rolling...";
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
        if (result) result.text = r.win ? "คุณชนะ" : "คุณแพ้";
    }

    // Quick-select bet amount
    public void pick10000() { cost = 10000; if (amountInput) amountInput.text = cost.ToString(); }
    public void pick50000() { cost = 50000; if (amountInput) amountInput.text = cost.ToString(); }
    public void pick100000() { cost = 100000; if (amountInput) amountInput.text = cost.ToString(); }

    public void pickHight_Even() { hightEven = true; hightOdd = lowEven = lowOdd = false; }
    public void pickHight_Odd() { hightOdd = true; hightEven = lowEven = lowOdd = false; }
    public void pickLow_Even() { lowEven = true; hightEven = hightOdd = lowOdd = false; }
    public void pickLow_Odd() { lowOdd = true; hightEven = hightOdd = lowEven = false; }

    private void Start()
    {
        for (int a = 0; a < BetButton.Length; a++)
        {
            int idx = a;
            BetButton[a].onClick.AddListener(() => OnButtonClicked(idx));
            BetButton[a].image.color = Color.white;
        }
        for (int b = 0; b < PriceButton.Length; b++)
        {
            int idx = b;
            PriceButton[b].onClick.AddListener(() => OnButtonClicked01(idx));
            PriceButton[b].image.color = Color.white;
        }
    }

    private void OnButtonClicked(int clickedIndex)
    {
        for (int i = 0; i < BetButton.Length; i++)
            BetButton[i].image.color = (i == clickedIndex) ? Color.gray : Color.white;
    }
    private void OnButtonClicked01(int clickedIndex)
    {
        for (int j = 0; j < PriceButton.Length; j++)
            PriceButton[j].image.color = (j == clickedIndex) ? Color.gray : Color.white;
    }
}
