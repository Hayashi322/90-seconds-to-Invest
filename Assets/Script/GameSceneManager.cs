using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSceneManager : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Image characterImage;
    public Sprite[] characterSprites;

    void Start()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance ไม่ถูกสร้าง ตรวจสอบว่าเริ่มจาก MainMenu หรือไม่");
            return;
        }

        // แสดงชื่อผู้เล่น
        playerNameText.text = PlayerData.Instance.playerName;

        // แสดงตัวละครตาม index ที่เลือกไว้
        int index = PlayerData.Instance.selectedCharacterIndex;
        if (index >= 0 && index < characterSprites.Length)
        {
            characterImage.sprite = characterSprites[index];
        }
        else
        {
            Debug.LogWarning("Index ตัวละครไม่ถูกต้อง");
        }
    }
}
