using System.Collections;
using UnityEngine;

public class NewsIconNotifier : MonoBehaviour
{
    public static NewsIconNotifier Instance { get; private set; }

    [Header("Icon Target (RectTransform)")]
    [SerializeField] private RectTransform iconTransform;

    [Header("Shake / Wiggle Settings")]
    [SerializeField] private float rotateAmplitude = 15f;   // องศาที่หมุนส่ายไปมา
    [SerializeField] private float rotateSpeed = 8f;        // ความเร็วการหมุน
    [SerializeField] private float scaleAmplitude = 0.1f;   // ขยาย/ย่อ เล็กน้อย
    [SerializeField] private float scaleSpeed = 3f;         // ความเร็วการขยาย/ย่อ

    private bool hasUnseenEvents = false;
    private Coroutine shakeRoutine;

    private Quaternion originalRotation;
    private Vector3 originalScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!iconTransform)
            iconTransform = transform as RectTransform;

        if (iconTransform)
        {
            originalRotation = iconTransform.localRotation;
            originalScale = iconTransform.localScale;
        }
    }

    private void OnEnable()
    {
        if (EventManagerNet.Instance != null)
            EventManagerNet.Instance.OnEventsChanged += HandleEventsChanged;
    }

    private void OnDisable()
    {
        if (EventManagerNet.Instance != null)
            EventManagerNet.Instance.OnEventsChanged -= HandleEventsChanged;

        StopShake();
    }

    /// <summary>
    /// ถูกเรียกอัตโนมัติเมื่อ EventManagerNet มีการเปลี่ยนชุดอีเวนต์
    /// </summary>
    private void HandleEventsChanged()
    {
        hasUnseenEvents = true;
        StartShake();
    }

    private void StartShake()
    {
        if (!iconTransform) return;

        if (shakeRoutine == null)
            shakeRoutine = StartCoroutine(ShakeLoop());
    }

    private void StopShake()
    {
        hasUnseenEvents = false;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (iconTransform)
        {
            iconTransform.localRotation = originalRotation;
            iconTransform.localScale = originalScale;
        }
    }

    private IEnumerator ShakeLoop()
    {
        float t = 0f;

        while (hasUnseenEvents)
        {
            t += Time.unscaledDeltaTime;  // ใช้ unscaled เพื่อไม่โดน Time.timeScale

            float rotZ = Mathf.Sin(t * rotateSpeed) * rotateAmplitude;
            float scaleFactor = 1f + Mathf.Sin(t * scaleSpeed) * scaleAmplitude;

            iconTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            iconTransform.localScale = originalScale * scaleFactor;

            yield return null;
        }

        // รีเซ็ตค่ากลับ
        iconTransform.localRotation = originalRotation;
        iconTransform.localScale = originalScale;

        shakeRoutine = null;
    }

    /// <summary>
    /// เรียกจากตอนผู้เล่นกดเปิดหน้า News แล้ว
    /// </summary>
    public void MarkEventsSeen()
    {
        StopShake();
    }
}