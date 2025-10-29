using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultsUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject rowPrefab; // มี TextMeshProUGUI 3 ช่อง: Rank, Name, NetWorth
    [SerializeField] private TextMeshProUGUI titleText;   // “สรุปผลรอบสุดท้าย”
    [SerializeField] private Button continueButton;       // ไป GameOver

    private void OnEnable()
    {
        BuildTable();
        if (continueButton) continueButton.onClick.AddListener(OnContinue);
    }

    private void OnDisable()
    {
        if (continueButton) continueButton.onClick.RemoveListener(OnContinue);
    }

    private void ClearChildren()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
    }

    private void BuildTable()
    {
        var mgr = GameResultManager.Instance;
        if (!mgr || mgr.results == null) return;

        ClearChildren();

        // คัดลอกมาจัดอันดับฝั่ง Client
        // ✅ ใหม่ (ทำงานได้จริง)
        var list = new List<PlayerResultNet>();

        foreach (var r in mgr.results)
        {
            list.Add(r);
        }

        list.Sort((a, b) => b.netWorth.CompareTo(a.netWorth));


        if (titleText)
            titleText.text = "สรุปผลรอบสุดท้าย";

        for (int i = 0; i < list.Count; i++)
        {
            var go = Instantiate(rowPrefab, content);
            var texts = go.GetComponentsInChildren<TextMeshProUGUI>();
            // สมมติลำดับ: [0]=Rank, [1]=Name, [2]=Worth
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString();
                texts[1].text = list[i].playerName.ToString();
                texts[2].text = $"{list[i].netWorth:N0} ฿";
            }
        }
    }

    private void OnContinue()
    {
        // ขอ Server พาไปฉาก GameOver (ประกาศผู้ชนะ)
        GameResultManager.Instance.ProceedToGameOverServerRpc();
    }
}
