using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.UI;

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
    [SerializeField] private TextMeshProUGUI enterMessageText;
    [SerializeField] private float enterMessageDuration = 3f;

    [Header("Bet")]
    [SerializeField] private int cost = 10000;
    private bool hightEven = false;
    private bool hightOdd = false;
    private bool lowEven = false;
    private bool lowOdd = false;

    private InventoryManager inv;
    private PlayerLawState lawState;

    // ===== Cooldown System =====
    private bool isCooldown = false;
    [SerializeField] private float cooldownTime = 30f;
    [SerializeField] private TextMeshProUGUI cooldownText;
    private float cooldownEndTime = -1f;

    private void OnEnable()
    {
        SetInteractable(false);
        StartCoroutine(BindLocalInventory());

        UpdateRollButtonState(); // ตรวจสอบสถานะ Roll ปุ่ม

        if (enterMessageText)
        {
            StopAllCoroutines();
            StartCoroutine(ShowEnterMessage());
        }
        else
        {
            if (result) StartCoroutine(ShowTempOn(result));
        }
    }

    private IEnumerator ShowEnterMessage()
    {
        enterMessageText.text = "เลือกลงทุนกับบริษัท Start Up";
        enterMessageText.gameObject.SetActive(true);

        yield return new WaitForSeconds(enterMessageDuration);

        enterMessageText.text = "";
        enterMessageText.gameObject.SetActive(false);
    }

    private IEnumerator ShowTempOn(TextMeshProUGUI target)
    {
        string backup = target.text;
        target.text = "เลือกลงทุนกับบริษัท Start Up";
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

        lawState = localObj.GetComponent<PlayerLawState>();

        OnCashChanged(inv.cash.Value, inv.cash.Value);
        SetInteractable(true);

        if (lawState != null && lawState.IsOwner)
        {
            lawState.EnterCasinoServerRpc();
        }
    }

    private void SetInteractable(bool canUse)
    {
        if (rollButton) rollButton.interactable = canUse;
        if (amountInput) amountInput.interactable = canUse;
    }

    private void OnCashChanged(double oldVal, double newVal)
    {
        if (money) money.text = $"{newVal:N0}";
    }

    // =====================================
    //           กดปุ่ม Roll
    // =====================================
    public void RollDice()
    {
        if (isCooldown) return;
        if (inv == null) return;

        int bet = cost;
        if (amountInput && int.TryParse(amountInput.text, out var custom) && custom > 0)
            bet = custom;

        if (inv.cash.Value < bet) return; // ป้องกันเงินไม่พอ

        isCooldown = true;
        cooldownEndTime = Time.time + cooldownTime;

        inv.PlaceBetServerRpc(bet, ResolveChoice());

        if (lawState != null && lawState.IsOwner)
        {
            lawState.NotifyCasinoRollServerRpc();
        }
    }

    // =====================================
    //     Update → นับถอยหลังคูลดาวน์
    // =====================================
    private void Update()
    {
        if (!isCooldown) return;

        float remaining = cooldownEndTime - Time.time;

        if (remaining <= 0)
        {
            isCooldown = false;
            if (cooldownText) cooldownText.text = "";
        }
        else
        {
            if (cooldownText)
                cooldownText.text = "ต้องรอ " + Mathf.CeilToInt(remaining) + " วินาที";
        }
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

        if (inv.bonus.Value > 0)
            result.text = "คุณได้เงินตอบแทน x" + inv.bonus.Value;
        else
            result.text = "คุณไม่ได้เงินตอบแทน";
    }

    // ================= Quick-select =================
    public void pick10000() { cost = 10000; if (amountInput) amountInput.text = cost.ToString(); UpdateRollButtonState(); }
    public void pick50000() { cost = 50000; if (amountInput) amountInput.text = cost.ToString(); UpdateRollButtonState(); }
    public void pick100000() { cost = 100000; if (amountInput) amountInput.text = cost.ToString(); UpdateRollButtonState(); }

    // ================= Bet button =================
    public void pickHight_Even() { hightEven = true; hightOdd = lowEven = lowOdd = false; UpdateRollButtonState(); }
    public void pickHight_Odd() { hightOdd = true; hightEven = lowEven = lowOdd = false; UpdateRollButtonState(); }
    public void pickLow_Even() { lowEven = true; hightEven = hightOdd = lowOdd = false; UpdateRollButtonState(); }
    public void pickLow_Odd() { lowOdd = true; hightEven = hightOdd = lowEven = false; UpdateRollButtonState(); }

    // ================= Update Roll Button State =================
    private void UpdateRollButtonState()
    {
        bool moneySelected = false;
        if (amountInput && int.TryParse(amountInput.text, out var val) && val > 0)
            moneySelected = true;

        bool betSelected = hightEven || hightOdd || lowEven || lowOdd;

        if (rollButton)
            rollButton.interactable = moneySelected && betSelected;
    }

    // ================= Input field callback =================
    public void OnAmountInputChanged(string value)
    {
        UpdateRollButtonState();
    }

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

    public void OnClickCloseCasino()
    {
        if (lawState != null && lawState.IsOwner)
        {
            lawState.ExitCasinoServerRpc();
        }

        gameObject.SetActive(false);
    }
}
