using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InGameEscMenu : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject panelRoot;          // ESCPanel
    [SerializeField] private CanvasGroup panelCanvasGroup;  // CanvasGroup บน ESCPanel
    [SerializeField] private RectTransform panelRect;       // RectTransform ของ ESCPanel

    [Tooltip("ตำแหน่งตอนซ่อน (ล่างจอ)")]
    [SerializeField] private Vector2 hiddenAnchoredPos = new Vector2(0, -600f);

    [Tooltip("ตำแหน่งตอนโชว์ (ตรงกลาง/ตำแหน่งปกติ)")]
    [SerializeField] private Vector2 shownAnchoredPos = Vector2.zero;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.25f;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;   // ปุ่ม "เล่นต่อ"
    [SerializeField] private Button exitButton;     // ปุ่ม "ออกไปหน้าเมนู"

    private bool isOpen = false;
    private Coroutine animRoutine = null;

    private void Awake()
    {
        // auto–fill ถ้าเธอลืมลาก
        if (panelRoot == null && panelCanvasGroup != null)
            panelRoot = panelCanvasGroup.gameObject;
        if (panelRoot == null && panelRect != null)
            panelRoot = panelRect.gameObject;

        if (panelRect == null && panelRoot != null)
            panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelCanvasGroup == null && panelRoot != null)
            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

        // สภาพเริ่มต้น = ซ่อน panel (แต่ไม่ปิด GameObject)
        SetHiddenStateInstant();

        if (resumeButton)
            resumeButton.onClick.AddListener(HideMenu);

        if (exitButton)
            exitButton.onClick.AddListener(OnExitClicked);
    }

    private void Update()
    {
        // กด ESC เพื่อเปิด/ปิดเมนู
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isOpen)
                ShowMenu();
            else
                HideMenu();
        }
    }

    // ====== public ======

    public void ShowMenu()
    {
        if (!panelRoot || !panelRect || !panelCanvasGroup) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        // ไม่ต้อง SetActive(true) เพราะเราไม่เคยปิดอยู่แล้ว
        animRoutine = StartCoroutine(AnimatePopup(show: true));
    }

    public void HideMenu()
    {
        if (!panelRoot || !panelRect || !panelCanvasGroup) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimatePopup(show: false));
    }

    // ====== animation ======

    private void SetHiddenStateInstant()
    {
        if (!panelRoot || !panelRect || !panelCanvasGroup) return;

        // ❌ ไม่ปิด GameObject แล้วนะ
        // panelRoot.SetActive(false);

        panelRect.anchoredPosition = hiddenAnchoredPos;
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;
        isOpen = false;
    }

    private IEnumerator AnimatePopup(bool show)
    {
        isOpen = show;

        Vector2 fromPos = show ? hiddenAnchoredPos : shownAnchoredPos;
        Vector2 toPos = show ? shownAnchoredPos : hiddenAnchoredPos;

        float fromAlpha = show ? 0f : 1f;
        float toAlpha = show ? 1f : 0f;

        if (show)
        {
            // ให้รับอินพุต/คลิก ตอนโชว์
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
        }

        float t = 0f;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime; // ใช้ unscaled เพื่อไม่พึ่ง Time.timeScale
            float k = Mathf.Clamp01(t / animDuration);

            panelRect.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            panelCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, k);

            yield return null;
        }

        panelRect.anchoredPosition = toPos;
        panelCanvasGroup.alpha = toAlpha;

        if (!show)
        {
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
            // ไม่ปิด GameObject เพื่อให้ Update() ยังทำงาน
            // panelRoot.SetActive(false);
        }

        animRoutine = null;
    }

    // ====== buttons ======

    private void OnExitClicked()
    {
        NetworkReturnToMenu.ReturnToMenu();
    }
}
