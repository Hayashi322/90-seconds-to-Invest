using UnityEngine;
using TMPro;
using System.Collections;
using Game.Economy;

public class casinoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dice1;
    [SerializeField] private TextMeshProUGUI dice2;
    [SerializeField] private TextMeshProUGUI money;
    [SerializeField] private TextMeshProUGUI result;

    [SerializeField] private int cost = 10000;

    [SerializeField] private bool hightEven = true;
    [SerializeField] private bool hightOdd = false;
    [SerializeField] private bool lowEven = false;
    [SerializeField] private bool lowOdd = false;

    public InventoryManager inventoryManager;

    private void OnEnable() { StartCoroutine(WaitAndBind()); }
    private IEnumerator WaitAndBind()
    {
        while (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null) break;
            yield return null;
        }
        inventoryManager.CasinoResultReceived += OnCasinoResult;
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.CasinoResultReceived -= OnCasinoResult;
    }

    private void Update()
    {
        if (inventoryManager != null && money)
            money.text = $"{inventoryManager.cash.Value:N0}";
    }

    public void RollDice()
    {
        if (inventoryManager == null) return;
        inventoryManager.PlaceBetServerRpc(cost, ResolveChoice());
        if (result) result.text = "Rolling...";
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
        if (result) result.text = r.win ? "YOU WIN" : "YOU LOSE";
    }

    public void pick10000() { cost = 10000; }
    public void pick50000() { cost = 50000; }
    public void pick100000() { cost = 100000; }
    public void pickHight_Even() { hightEven = true; hightOdd = lowEven = lowOdd = false; }
    public void pickHight_Odd() { hightOdd = true; hightEven = lowEven = lowOdd = false; }
    public void pickLow_Even() { lowEven = true; hightEven = hightOdd = lowOdd = false; }
    public void pickLow_Odd() { lowOdd = true; hightEven = hightOdd = lowEven = false; }
}
