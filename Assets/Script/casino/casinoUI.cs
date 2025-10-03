using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class casinoUI : MonoBehaviour
{
    [SerializeField] private bool is10000 = true;
    [SerializeField] private bool is50000 = false;
    [SerializeField] private bool is100000 = false;

    [SerializeField] private TextMeshProUGUI dice1;
    [SerializeField] private TextMeshProUGUI dice2;

    [SerializeField] private TextMeshProUGUI money;
    [SerializeField] private TextMeshProUGUI result;

    [SerializeField] private bool hightEven = true;
    [SerializeField] private bool hightOdd = false;
    [SerializeField] private bool lowEven = false;
    [SerializeField] private bool lowOdd = false;
     public void rollDice()
    {
        int dice1Number = Random.Range(1, 7); // สุ่มเลข 1 ถึง 6 (7 ไม่ถูกรวม)
        int dice2Number = Random.Range(1, 7);
        dice1.text = dice1Number.ToString("F0");
        dice2.text = dice2Number.ToString("F0");
        Debug.Log(dice1Number.ToString());
        Debug.Log(dice2Number.ToString());
        float sum = dice1Number + dice2Number;
        if(sum%2 == 0 && sum >= 6 && hightEven==true)
        {
            result.text = ("YOU WIN");
        }
        else if (sum % 2 != 0 && sum >= 6 && hightOdd == true)
        {
            result.text = ("YOU WIN");
        }
        else if (sum % 2 == 0 && sum < 6 && lowEven == true)
        {
            result.text = ("YOU WIN");
        }
       else if (sum % 2 != 0 && sum < 6 && lowOdd == true)
        {
            result.text = ("YOU WIN");
        }
        else { result.text = ("YOU LOSE"); }
        sum = 0;
    }
    public void pick10000()
    {
        is10000 = true;
        is50000 = false;
        is100000 = false;
    }
    public void pick50000()
    {
        is10000 = false;
        is50000 = true;
        is100000 = false;
    }
    public void pick100000()
    {
        is10000 = false;
        is50000 = false;
        is100000 = true;
    }

    public void pickHight_Even()
    {
        hightEven = true;
        hightOdd = false;
        lowEven = false;
        lowOdd = false;
    }
    public void pickHight_Odd()
    {
        hightEven = false;
        hightOdd = true;
        lowEven = false;
        lowOdd = false;
    }
    public void pickLow_Even()
    {
        hightEven = false;
        hightOdd = false;
        lowEven = true;
        lowOdd = false;
    }
    public void pickLow_Odd()
    {
        hightEven = false;
        hightOdd = false;
        lowEven = false;
        lowOdd = true;
    }
}
