using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NameSaveUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private const string PlayerNameKey = "player_name";

    private void Start()
    {
        // โหลดชื่อที่เคยเซฟไว้ (ถ้ามี)
        string savedName = PlayerPrefs.GetString(PlayerNameKey, "");
        if (nameInput) nameInput.text = savedName;

        if (saveButton) saveButton.onClick.AddListener(SaveName);
    }

    private void SaveName()
    {
        string playerName = nameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            if (statusText) statusText.text = "⚠️ กรุณาใส่ชื่อก่อนบันทึก";
            return;
        }

        // บันทึกชื่อไว้ใน PlayerPrefs
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();

        if (statusText)
        {
            statusText.text = $"✅ บันทึกชื่อเรียบร้อย: {playerName}";
            statusText.color = new Color(0.2f, 0.9f, 0.3f);
        }
    }
}
