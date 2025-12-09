using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingCashText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Animation")]
    [SerializeField] private float moveUpDistance = 40f;   // ลอยขึ้นกี่พิกเซล
    [SerializeField] private float duration = 0.8f;        // เวลาทั้งหมดของอนิเมชัน
    [SerializeField] private AnimationCurve alphaCurve = null;

    private RectTransform rectTransform;
    private Color gainColor = Color.green;
    private Color lossColor = Color.red;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (!text) text = GetComponent<TextMeshProUGUI>();

        // ถ้าไม่ได้เซ็ตใน Inspector ให้ใช้ EaseInOut ดีฟอลต์
        if (alphaCurve == null)
        {
            alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }

    /// <summary>
    /// เมธอดที่ CashUI เรียก → เซ็ตข้อความ + สี แล้วเริ่มอนิเมชัน
    /// </summary>
    public void SetText(string value, bool isGain)
    {
        if (!text) return;

        text.text = value;
        text.color = isGain ? gainColor : lossColor;

        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, moveUpDistance);

        float t = 0f;
        Color baseColor = text.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / duration);

            // เลื่อนขึ้น
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, normalized);
            // เฟด alpha
            float a = alphaCurve.Evaluate(normalized);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

            yield return null;
        }

        Destroy(gameObject);
    }
}
