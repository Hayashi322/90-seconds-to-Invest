using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuspectButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    private PlayerLawState target;
    private Action<PlayerLawState> onClick;

    /// <summary>
    /// ให้ PoliceStationUI เรียกตอนสร้างปุ่ม
    /// </summary>
    public void Setup(PlayerLawState target, Action<PlayerLawState> onClick)
    {
        this.target = target;
        this.onClick = onClick;

        if (!button) button = GetComponent<Button>();

        // -----------------------------
        // 1) ดึง sprite ของตัวละครผู้เล่น
        // -----------------------------
        Sprite portrait = null;

        if (target != null)
        {
            // PlayerLawState อยู่บน GameObject เดียวกับ HeroControllerNet
            var hero = target.GetComponent<HeroControllerNet>();
            if (hero != null)
            {
                // ใช้ SpriteRenderer ของตัวละครในฉากจริง
                var sr = hero.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    portrait = sr.sprite;
            }
        }

        if (iconImage != null)
        {
            if (portrait != null)
            {
                iconImage.sprite = portrait;
                iconImage.enabled = true;
            }
            else
            {
                // ถ้าไม่มีรูป ก็ตั้งให้โปร่งใสหรือไอคอน default ตามใจเลย
                iconImage.enabled = false;
            }
        }

        // -----------------------------
        // 2) แสดงชื่อ / ไอดีผู้เล่น (แล้วแต่จะใช้)
        // -----------------------------
        if (nameText != null)
        {
            // ตัวอย่าง: P0, P1, ...
            nameText.text = $"P{target?.OwnerClientId ?? 0}";
        }

        // -----------------------------
        // 3) Hook ปุ่มกด
        // -----------------------------
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (this.target != null && this.onClick != null)
                    this.onClick(this.target);
            });
        }
    }
}
