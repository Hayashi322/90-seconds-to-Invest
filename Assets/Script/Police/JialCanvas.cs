using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JialCanvas : MonoBehaviour
{
    public static JialCanvas Instance;

    [Header("UI Components")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text timerText;

    private float remainingTime;
    private bool counting;

    private void Awake()
    {
        Instance = this;
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
    }

    public void Show(float duration, string reason)
    {
        remainingTime = duration;
        reasonText.text = $"สาเหตุ: {reason}";
        titleText.text = "คุณถูกเชิญมาออกรายการ";
        panelGroup.gameObject.SetActive(true);

        // ★ บล็อคการคลิก UI อื่น
        panelGroup.blocksRaycasts = true;
        panelGroup.interactable = true;

        counting = true;

        // start fade-in
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(panelGroup, 0f, 1f, 0.4f));
    }

    private void Update()
    {
        if (!counting) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            counting = false;
            timerText.text = "";

            // fade out before hiding
            StartCoroutine(FadeOutAndHide());
        }
        else
        {
            timerText.text = $"จะถ่ายเสร็จใน {remainingTime:F0} วินาที";
        }
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return FadeCanvas(panelGroup, 1f, 0f, 0.4f);
        // ★ ปล่อยการคลิก UI อื่นหลังปิด
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;
        panelGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float duration)
    {
        cg.alpha = from;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to; 
    }
}//
