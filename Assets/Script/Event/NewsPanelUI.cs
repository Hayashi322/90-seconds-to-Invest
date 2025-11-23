using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsPanelUI : MonoBehaviour
{
    [Header("Root Popup Panel")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("Card 1")]
    [SerializeField] private RectTransform card1Rect;
    [SerializeField] private Image newsImage1;
    [SerializeField] private TMP_Text newsText1;

    [Header("Card 2")]
    [SerializeField] private RectTransform card2Rect;
    [SerializeField] private Image newsImage2;
    [SerializeField] private TMP_Text newsText2;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float slideDuration = 0.30f;
    [SerializeField] private float cardStartOffsetY = -400f;

    private Vector2 card1TargetPos;
    private Vector2 card2TargetPos;

    private bool isAnimating = false;
    private bool isOpen = false;

    private void Start()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (card1Rect != null) card1TargetPos = card1Rect.anchoredPosition;
        if (card2Rect != null) card2TargetPos = card2Rect.anchoredPosition;

        if (EventManagerNet.Instance != null)
            EventManagerNet.Instance.OnEventsChanged += RefreshUI;
    }

    private void OnDestroy()
    {
        if (EventManagerNet.Instance != null)
            EventManagerNet.Instance.OnEventsChanged -= RefreshUI;
    }

    // เรียกจากปุ่มไอคอนข่าว
    public void OpenNews()
    {
        if (isAnimating || isOpen) return;

        RefreshUI();
        StartCoroutine(ShowRoutine());
    }

    // เรียกจากปุ่มกากบาท
    public void CloseNews()
    {
        if (isAnimating || !isOpen) return;

        StartCoroutine(HideRoutine());
    }

    private void RefreshUI()
    {
        if (EventManagerNet.Instance == null) return;

        IReadOnlyList<EventConfig> eventsThisTurn = EventManagerNet.Instance.GetCurrentEvents();

        // ข่าวช่องที่ 1
        if (eventsThisTurn.Count > 0)
        {
            var cfg = eventsThisTurn[0];
            if (newsImage1) newsImage1.sprite = cfg.image;
            if (newsText1) newsText1.text = cfg.title;
        }
        else
        {
            if (newsText1) newsText1.text = "ไม่มีข่าว";
        }

        // ข่าวช่องที่ 2
        if (eventsThisTurn.Count > 1)
        {
            var cfg = eventsThisTurn[1];
            if (newsImage2) newsImage2.sprite = cfg.image;
            if (newsText2) newsText2.text = cfg.title;
        }
        else
        {
            if (newsText2) newsText2.text = "";
        }
    }

    // ===== Animation =====
    private IEnumerator ShowRoutine()
    {
        isAnimating = true;
        isOpen = true;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        // ตั้งต้นให้การ์ดอยู่ต่ำลง
        if (card1Rect != null)
            card1Rect.anchoredPosition = card1TargetPos + new Vector2(0f, cardStartOffsetY);
        if (card2Rect != null)
            card2Rect.anchoredPosition = card2TargetPos + new Vector2(0f, cardStartOffsetY);

        float t = 0f;

        // Fade-in + slide-up พร้อมกัน
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
            float a = Mathf.Clamp01(t);

            if (rootCanvasGroup != null)
                rootCanvasGroup.alpha = a;

            float slideT = Mathf.Clamp01(t / (slideDuration / fadeDuration));

            if (card1Rect != null)
                card1Rect.anchoredPosition = Vector2.Lerp(
                    card1TargetPos + new Vector2(0f, cardStartOffsetY),
                    card1TargetPos,
                    slideT
                );

            if (card2Rect != null)
                card2Rect.anchoredPosition = Vector2.Lerp(
                    card2TargetPos + new Vector2(0f, cardStartOffsetY),
                    card2TargetPos,
                    slideT
                );

            yield return null;
        }

        if (rootCanvasGroup != null)
            rootCanvasGroup.alpha = 1f;

        if (card1Rect != null) card1Rect.anchoredPosition = card1TargetPos;
        if (card2Rect != null) card2Rect.anchoredPosition = card2TargetPos;

        isAnimating = false;
    }

    private IEnumerator HideRoutine()
    {
        isAnimating = true;

        float startAlpha = rootCanvasGroup != null ? rootCanvasGroup.alpha : 1f;
        float t = 0f;

        // Fade-out + slide-down
        Vector2 card1Start = card1Rect != null ? card1Rect.anchoredPosition : Vector2.zero;
        Vector2 card2Start = card2Rect != null ? card2Rect.anchoredPosition : Vector2.zero;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
            float a = Mathf.Clamp01(1f - t);

            if (rootCanvasGroup != null)
                rootCanvasGroup.alpha = a * startAlpha;

            float slideT = Mathf.Clamp01(t / (slideDuration / fadeDuration));

            if (card1Rect != null)
                card1Rect.anchoredPosition = Vector2.Lerp(
                    card1Start,
                    card1TargetPos + new Vector2(0f, cardStartOffsetY),
                    slideT
                );

            if (card2Rect != null)
                card2Rect.anchoredPosition = Vector2.Lerp(
                    card2Start,
                    card2TargetPos + new Vector2(0f, cardStartOffsetY),
                    slideT
                );

            yield return null;
        }

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (rootPanel != null)
            rootPanel.SetActive(false);

        isAnimating = false;
        isOpen = false;
    }
}
