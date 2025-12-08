using System.Collections;
using TMPro;
using UnityEngine;

public class AverageStockGroupUI : MonoBehaviour
{
    [Header("Text ค่าเฉลี่ยหุ้น เรียงตามลำดับ: PTT, KBANK, AOT, BDMS, DELTA, CPNREIT")]
    [SerializeField] private TextMeshProUGUI[] avgCostTexts;

    private InventoryManager inv;

    // ลำดับชื่อต้องตรงกับที่เราลาก Text เข้าไปใน avgCostTexts
    private readonly string[] stockNames =
    {
        "PTT",
        "KBANK",
        "AOT",
        "BDMS",
        "DELTA",
        "CPNREIT"
    };

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        while (InventoryManager.Instance == null)
            yield return null;

        inv = InventoryManager.Instance;
    }

    private void Update()
    {
        if (inv == null) return;

        int count = Mathf.Min(avgCostTexts.Length, stockNames.Length);

        for (int i = 0; i < count; i++)
        {
            var txt = avgCostTexts[i];
            if (!txt) continue;

            double avg = inv.GetStockAverageCost(stockNames[i]);
            txt.text = avg > 0 ? avg.ToString("N2") : "-";
        }
    }
}
