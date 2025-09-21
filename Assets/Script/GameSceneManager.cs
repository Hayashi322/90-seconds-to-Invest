using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameSceneManager : MonoBehaviour
{
    [Header("Character Data")]
    public Sprite[] characterSprites;
    public Image uiCharacterImage;

    [Header("Runtime refs (auto if not set)")]
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Transform playerTransform;

    public float targetSize = 1.5f; // ความสูงเป้าหมาย (world unit)
    bool _applied;

    void OnEnable()
    {
        HeroControllerNet.LocalPlayerSpawned += OnLocalPlayerSpawned;
    }
    void OnDisable()
    {
        HeroControllerNet.LocalPlayerSpawned -= OnLocalPlayerSpawned;
    }

    void Start()
    {
        // ลองหาให้ก่อนเผื่อผูกไว้ในซีน
        TryAutoHookPlayer();
        ApplySelectedCharacterIfReady();
    }

    void OnLocalPlayerSpawned(HeroControllerNet hero)
    {
        // bind อัตโนมัติเมื่อ player โผล่มา (กรณี spawn ตอนรัน)
        if (!playerRenderer) playerRenderer = hero.GetComponentInChildren<SpriteRenderer>(true);
        if (!playerTransform && playerRenderer) playerTransform = playerRenderer.transform;
        ApplySelectedCharacterIfReady();
    }

    void TryAutoHookPlayer()
    {
        if (!playerRenderer)
        {
            // 1) จาก local player object (Netcode)
            var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
            if (localObj)
                playerRenderer = localObj.GetComponentInChildren<SpriteRenderer>(true);

            // 2) fallback: จาก tag "Player"
            if (!playerRenderer)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go) playerRenderer = go.GetComponentInChildren<SpriteRenderer>(true);
            }
        }
        if (!playerTransform && playerRenderer) playerTransform = playerRenderer.transform;
    }

    void ApplySelectedCharacterIfReady()
    {
        if (_applied) return;

        if (PlayerData.Instance == null)
        {
            Debug.LogError("PlayerData.Instance ไม่พบ (ควรเริ่มจากหน้าเมนู)");
            return;
        }
        if (!playerRenderer || !playerTransform)
        {
            Debug.LogWarning("ยังไม่พบ playerRenderer/playerTransform จะรอตอน local player spawn");
            return;
        }

        int index = PlayerData.Instance.selectedCharacterIndex;
        if (index < 0 || index >= characterSprites.Length)
        {
            Debug.LogWarning("Index ตัวละครไม่ถูกต้อง: " + index);
            return;
        }

        // UI
        if (uiCharacterImage) uiCharacterImage.sprite = characterSprites[index];

        // Player sprite
        var selectedSprite = characterSprites[index];
        playerRenderer.sprite = selectedSprite;

        // ปรับสเกลให้สูง = targetSize (อ่านจาก sprite ที่เพิ่งเซ็ต)
        var sprite = playerRenderer.sprite;
        float h = sprite.bounds.size.y;
        if (h > 0f)
        {
            float scale = targetSize / h;
            playerTransform.localScale = Vector3.one * scale;
        }
        else
        {
            Debug.LogWarning("Sprite height is 0?");
        }

        _applied = true;
    }
}
