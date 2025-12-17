using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class FloatingCashText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("Animation")]
    [SerializeField] private float moveUpDistance = 40f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private AnimationCurve alphaCurve = null;

    [Header("Sound FX")]
    [SerializeField] private AudioClip gainSfx;   // เสียงเงินเข้า
    [SerializeField] private AudioClip lossSfx;   // เสียงเงินออก
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private RectTransform rectTransform;
    private AudioSource audioSource;

    private Color gainColor = Color.green;
    private Color lossColor = Color.red;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        audioSource = GetComponent<AudioSource>();

        if (!text) text = GetComponent<TextMeshProUGUI>();

        if (alphaCurve == null)
        {
            alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }

        // ตั้งค่า AudioSource ให้เหมาะกับ SFX
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // UI = 2D sound
    }

    /// <summary>
    /// CashUI เรียก → เซ็ตข้อความ สี และเสียง
    /// </summary>
    public void SetText(string value, bool isGain)
    {
        if (!text) return;

        text.text = value;
        text.color = isGain ? gainColor : lossColor;

        PlaySound(isGain);
        StartCoroutine(PlayAnimation());
    }

    private void PlaySound(bool isGain)
    {
        AudioClip clip = isGain ? gainSfx : lossSfx;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume);
        }
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

            rectTransform.anchoredPosition =
                Vector2.Lerp(startPos, endPos, normalized);

            float a = alphaCurve.Evaluate(normalized);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

            yield return null;
        }

        Destroy(gameObject);
    }
}
