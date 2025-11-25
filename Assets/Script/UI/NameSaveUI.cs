using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class NameSaveUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Flash Warning")]
    [SerializeField] private Image highlightImage;          // กรอบ/พื้นหลังช่องชื่อ
    [SerializeField] private Color warningColor = new Color(1f, 0.5f, 0.5f);
    [SerializeField] private float flashTime = 0.15f;
    [SerializeField] private int flashCount = 3;

    [Header("Name Limit")]
    [SerializeField] private int maxNameLength = 14;        // ✅ จำกัดชื่อไม่เกิน 14 ตัวอักษร

    private const string PlayerNameKey = "player_name";

    private Color _originalColor;
    private Coroutine _flashRoutine;

    private void Start()
    {
        // ตั้ง limit ให้ช่องกรอกชื่อ
        if (nameInput)
        {
            nameInput.characterLimit = maxNameLength;
        }

        // โหลดชื่อที่เคยเซฟไว้ (ถ้ามี)
        string savedName = PlayerPrefs.GetString(PlayerNameKey, "");

        // ถ้าชื่อที่เคยเซฟยาวเกิน – ตัดให้เหลือไม่เกิน maxNameLength
        if (!string.IsNullOrEmpty(savedName) &&
            maxNameLength > 0 &&
            savedName.Length > maxNameLength)
        {
            savedName = savedName.Substring(0, maxNameLength);
        }

        if (nameInput) nameInput.text = savedName;

        // เตรียม target สำหรับกระพริบ
        if (!highlightImage && nameInput)
            highlightImage = nameInput.GetComponent<Image>();

        if (highlightImage)
            _originalColor = highlightImage.color;

        if (saveButton) saveButton.onClick.AddListener(SaveName);

        // 🔹 sync เข้า PlayerData ถ้ามี
        if (PlayerData.Instance != null && !string.IsNullOrWhiteSpace(savedName))
        {
            PlayerData.Instance.playerName = savedName;
        }
    }

    private void SaveName()
    {
        string playerName = nameInput ? nameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(playerName))
        {
            ShowEmptyNameWarning();
            return;
        }

        // ✅ กันเหนียว ตัดให้ไม่เกิน maxNameLength อีกชั้น
        if (maxNameLength > 0 && playerName.Length > maxNameLength)
        {
            playerName = playerName.Substring(0, maxNameLength);
            if (nameInput) nameInput.text = playerName; // อัปเดตกลับให้ผู้เล่นเห็น
        }

        // บันทึกชื่อไว้ใน PlayerPrefs
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();

        // 🔹 sync เข้า PlayerData
        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.SetPlayerName(playerName);
        }

        if (statusText)
        {
            statusText.text = $"✅ บันทึกชื่อเรียบร้อย: {playerName}";
            statusText.color = new Color(0.2f, 0.9f, 0.3f);
        }
    }

    /// <summary>
    /// ให้ปุ่ม เล่น / เข้าร่วม เรียกก่อนเริ่มเกม
    /// - ถ้าไม่ใส่ชื่อ → กระพริบเตือน + return false
    /// - ถ้ามีชื่อ → เซฟให้ด้วย แล้ว return true
    /// </summary>
    public bool EnsureNameSavedOrWarn()
    {
        string playerName = nameInput ? nameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(playerName))
        {
            ShowEmptyNameWarning();
            return false;
        }

        // ✅ ตัดไม่เกิน maxNameLength
        if (maxNameLength > 0 && playerName.Length > maxNameLength)
        {
            playerName = playerName.Substring(0, maxNameLength);
            if (nameInput) nameInput.text = playerName;
        }

        // มีชื่อแล้ว แต่ยังไม่กดเซฟ → เซฟให้เลย
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();

        if (PlayerData.Instance != null)
        {
            PlayerData.Instance.SetPlayerName(playerName);
        }

        return true;
    }

    private void ShowEmptyNameWarning()
    {
        if (statusText)
        {
            statusText.text = "⚠️ กรุณาใส่ชื่อก่อนเล่น";
            statusText.color = Color.red;
        }

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);

        _flashRoutine = StartCoroutine(FlashHighlight());
    }

    private IEnumerator FlashHighlight()
    {
        if (!highlightImage)
            yield break;

        for (int i = 0; i < flashCount; i++)
        {
            highlightImage.color = warningColor;
            yield return new WaitForSeconds(flashTime);
            highlightImage.color = _originalColor;
            yield return new WaitForSeconds(flashTime);
        }

        _flashRoutine = null;
    }
}
