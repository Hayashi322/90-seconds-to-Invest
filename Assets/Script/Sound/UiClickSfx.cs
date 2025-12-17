using UnityEngine;

public class UiClickSfx : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    // กันไม่ให้มีหลายตัว
    private static UiClickSfx instance;

    private void Awake()
    {
        // ถ้ามีตัวอื่นอยู่แล้ว → ทำลายตัวนี้
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // อยู่ข้ามซีน
        DontDestroyOnLoad(gameObject);

        // ตั้งค่า AudioSource
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
            if (!audioSource)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D sound (UI)
    }

    /// <summary>
    /// เรียกจากปุ่ม UI OnClick()
    /// </summary>
    public void PlayClick()
    {
        if (!clickClip) return;
        audioSource.PlayOneShot(clickClip, volume);
    }
}
