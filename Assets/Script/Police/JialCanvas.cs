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

    [Header("Shake Settings")]
    [SerializeField] private RectTransform shakeTarget;   // panel ที่จะสั่น (เช่น Panel ทั้งกล่อง)
    [SerializeField] private float shakeDuration = 0.5f;  // เวลาสั่น
    [SerializeField] private float shakeMagnitude = 20f;  // ระยะสั่น
    [SerializeField] private float shakeFrequency = 40f;  // ความถี่ในการอัปเดต

    private float remainingTime;
    private bool counting;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        Instance = this;

        if (!panelGroup)
            panelGroup = GetComponent<CanvasGroup>();

        // เริ่มต้นปิดไว้ก่อน
        panelGroup.alpha = 0f;
        panelGroup.gameObject.SetActive(false);
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;
    }

    public void Show(float duration, string reason)
    {
        remainingTime = duration;

        if (reasonText)
            reasonText.text = $"สาเหตุ: {reason}";

        if (titleText)
            titleText.text = "คุณถูกเชิญมาออกรายการ";

        panelGroup.gameObject.SetActive(true);

        // บล็อกการคลิก UI อื่น
        panelGroup.blocksRaycasts = true;
        panelGroup.interactable = true;

        counting = true;

        // เริ่ม fade-in + สั่นใหม่
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(panelGroup, 0f, 1f, 0.4f));

        if (shakeTarget != null)
        {
            shakeRoutine = StartCoroutine(ShakeRoutine());
        }
    }

    private void Update()
    {
        if (!counting) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            counting = false;
            if (timerText)
                timerText.text = "";

            // fade out before hiding
            StartCoroutine(FadeOutAndHide());
        }
        else
        {
            if (timerText)
                timerText.text = $"จะถ่ายเสร็จใน {remainingTime:F0} วินาที";
        }
    }

    private IEnumerator FadeOutAndHide()
    {
        yield return FadeCanvas(panelGroup, 1f, 0f, 0.4f);

        // ปล่อยการคลิก UI อื่นหลังปิด
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

    private IEnumerator ShakeRoutine()
    {
        Vector2 originalPos = shakeTarget.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / shakeDuration);

            // ค่อย ๆ ลดความแรงลงเรื่อย ๆ
            float currentMag = Mathf.Lerp(shakeMagnitude, 0f, progress);

            float angle = Random.value * Mathf.PI * 2f;
            float offsetX = Mathf.Cos(angle) * currentMag;
            float offsetY = Mathf.Sin(angle) * currentMag;

            shakeTarget.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

            yield return new WaitForSeconds(1f / shakeFrequency);
        }

        shakeTarget.anchoredPosition = originalPos;
    }
}
