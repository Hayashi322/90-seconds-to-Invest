using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameSceneManager : MonoBehaviour
{
    [Header("Character Data")]
    public Sprite[] characterSprites;      // เอาไว้ใช้กับ UI อย่างเดียว
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

        if (!playerRenderer || !playerTransform)
        {
            Debug.LogWarning("ยังไม่พบ playerRenderer/playerTransform จะรอตอน local player spawn");
            return;
        }

        // 🔹 หาค่า index เพื่อใช้กับ UI: พยายามอ่านจาก HeroControllerNet ก่อน
        int index = -1;
        var hero = playerRenderer.GetComponentInParent<HeroControllerNet>();
        if (hero != null)
        {
            index = hero.CharacterIndex.Value;
        }
        else if (PlayerData.Instance != null)
        {
            index = PlayerData.Instance.selectedCharacterIndex;
        }

        // 🔹 เซ็ตเฉพาะ UI ถ้า index อยู่ในช่วง
        if (uiCharacterImage && index >= 0 && index < characterSprites.Length)
        {
            uiCharacterImage.sprite = characterSprites[index];
        }

        // 🔹 ไม่แตะ playerRenderer.sprite เลย ปล่อยให้ HeroControllerNet จัดการ
        //    ใช้ sprite ปัจจุบันเพื่อคำนวณสเกลเฉย ๆ
        var sprite = playerRenderer.sprite;
        if (sprite != null)
        {
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
        }

        _applied = true;
    }
}
