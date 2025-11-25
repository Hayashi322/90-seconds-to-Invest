using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LocalPlayerAvatarUI : MonoBehaviour
{
    [Header("UI Avatar")]
    [SerializeField] private Image avatarImage;            // รูปหัวมุมซ้ายล่าง (UI)
    [SerializeField] private Sprite[] characterIcons;      // ไอคอนตามลำดับเดียวกับ characterSpritesInGame

    private HeroControllerNet _localHero;

    private void OnEnable()
    {
        HeroControllerNet.LocalPlayerSpawned += OnLocalHeroSpawned;
        HeroControllerNet.LocalPlayerDespawned += OnLocalHeroDespawned;
    }

    private void OnDisable()
    {
        HeroControllerNet.LocalPlayerSpawned -= OnLocalHeroSpawned;
        HeroControllerNet.LocalPlayerDespawned -= OnLocalHeroDespawned;

        if (_localHero != null)
        {
            _localHero.CharacterIndex.OnValueChanged -= OnCharacterIndexChanged;
            _localHero = null;
        }
    }

    private void OnLocalHeroSpawned(HeroControllerNet hero)
    {
        // ได้ hero ของ "เรา" แล้ว
        _localHero = hero;

        // ฟังค่า CharacterIndex เปลี่ยน
        _localHero.CharacterIndex.OnValueChanged += OnCharacterIndexChanged;

        // เซ็ตครั้งแรกตามค่าปัจจุบัน
        OnCharacterIndexChanged(-1, _localHero.CharacterIndex.Value);
    }

    private void OnLocalHeroDespawned()
    {
        if (_localHero != null)
        {
            _localHero.CharacterIndex.OnValueChanged -= OnCharacterIndexChanged;
            _localHero = null;
        }
    }

    private void OnCharacterIndexChanged(int oldIndex, int newIndex)
    {
        if (!avatarImage) return;
        if (newIndex < 0 || newIndex >= characterIcons.Length) return;

        avatarImage.sprite = characterIcons[newIndex];
    }
}
