using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";

    private void Start()
    {
        LoadSettings();

        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    public void OpenSettings()
    {
        LoadSettings();                 // ⭐ โหลดทุกครั้งที่เปิด
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    private void LoadSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFXKey, 1f);

        // ตั้งค่า Slider (ไม่ให้ trigger ซ้ำ)
        musicSlider.SetValueWithoutNotify(musicVol);
        sfxSlider.SetValueWithoutNotify(sfxVol);

        // ตั้งค่าเสียงจริง
        ApplyMusicVolume(musicVol);
        ApplySFXVolume(sfxVol);
    }

    private void OnMusicChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    private void OnSFXChanged(float value)
    {
        ApplySFXVolume(value);
        PlayerPrefs.SetFloat(SFXKey, value);
    }

    private void ApplyMusicVolume(float value)
    {
        if (BgmManager.Instance != null)
            BgmManager.Instance.GetComponent<AudioSource>().volume = value;
    }

    private void ApplySFXVolume(float value)
    {
        if (UiSfxManager.Instance != null)
            UiSfxManager.Instance.GetComponent<AudioSource>().volume = value;
    }
}
