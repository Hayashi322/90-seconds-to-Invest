using UnityEngine;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    public Sprite[] characterSprites;
    public Image uiCharacterImage;
    public SpriteRenderer playerRenderer;
    public Transform playerTransform;

    public float targetSize = 1.5f; // ความสูงเป้าหมาย (world unit)

    void Start()
    {
        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance ไม่พบ (ควรเริ่มจากหน้าเมนู)");
            return;
        }

        int index = PlayerData.Instance.selectedCharacterIndex;

        if (index >= 0 && index < characterSprites.Length)
        {
            // UI
            uiCharacterImage.sprite = characterSprites[index];

            // Player
            Sprite selectedSprite = characterSprites[index];
            playerRenderer.sprite = selectedSprite;

            // ปรับขนาดให้สูง = targetSize
            float currentHeight = selectedSprite.bounds.size.y;
            float scaleFactor = targetSize / currentHeight;
            playerTransform.localScale = Vector3.one * scaleFactor;
        }
        else
        {
            Debug.LogWarning("Index ตัวละครไม่ถูกต้อง: " + index);
        }
    }
}

