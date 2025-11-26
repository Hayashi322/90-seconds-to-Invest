using UnityEngine;

public class UiSfxManager : MonoBehaviour
{
    public static UiSfxManager Instance { get; private set; }

    [Header("Audio Source (สำหรับ SFX)")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip sellClip;     // เสียงปุ่ม Sell
    [SerializeField] private AudioClip diceClip;     // เสียงปุ่มทอยเต๋า

    private void Awake()
    {
        // ถ้าจะกันไม่ให้มีหลายตัวในซีนเดียว (กันเผลอวางซ้ำ)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!sfxSource)
        {
            // ถ้าไม่มี AudioSource ติดอยู่ ให้ลองดึงจากตัวเองก่อน
            sfxSource = GetComponent<AudioSource>();
            if (!sfxSource)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    public void PlaySell()
    {
        PlayOneShot(sellClip);
    }

    public void PlayDice()
    {
        PlayOneShot(diceClip);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!clip || !sfxSource) return;
        sfxSource.PlayOneShot(clip);
    }
}
