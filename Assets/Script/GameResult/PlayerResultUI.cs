using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResultUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform root;
    [SerializeField] private Image avatarImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private Image rankBadge;

    [Header("Anim Settings")]
    [SerializeField] private float moneyAnimDuration = 1.0f;

    private void Reset()
    {
        root = GetComponent<RectTransform>();
    }

    /// <summary>
    /// ตั้งค่า UI เริ่มต้น (ก่อนลุ้นหวย)
    /// </summary>
    public void Setup(Sprite avatar, string playerName, long initialCash, int rank)
    {
        if (!root) root = GetComponent<RectTransform>();

        if (avatarImage) avatarImage.sprite = avatar;
        if (nameText) nameText.text = playerName;

        // ✅ แสดงเงินเป็น THB 1,234,567
        if (cashText) cashText.text = $"THB {initialCash:N0}";

        if (rankBadge)
            rankBadge.gameObject.SetActive(false);
    }

    public void SetRankBadge(Sprite badgeSprite)
    {
        if (!rankBadge || !badgeSprite) return;
        rankBadge.sprite = badgeSprite;
        rankBadge.gameObject.SetActive(true);
    }

    /// <summary>
    /// โผล่ขึ้นมาเฉย ๆ แล้ววิ่งเงินจาก 0 → initialCash
    /// </summary>
    public IEnumerator ShowWithSlideAndMoney(long initialCash)
    {
        yield return AnimateMoney(0, initialCash);
    }

    /// <summary>
    /// ใช้ตอนหลังหวยออกเพื่ออัปเดตเงินเป็นค่าหลังหวย
    /// </summary>
    public IEnumerator UpdateMoneyTo(long newCash)
    {
        long current = 0;

        if (cashText)
        {
            // ✅ ตัด THB และคอมมาออก แล้ว Parse
            var raw = cashText.text
                .Replace("THB", "")
                .Replace(",", "")
                .Trim();

            long.TryParse(raw, out current);
        }

        yield return AnimateMoney(current, newCash);
    }

    /// <summary>
    /// แอนิเมชันเพิ่มเงินแบบลื่น ๆ
    /// </summary>
    private IEnumerator AnimateMoney(long from, long to)
    {
        if (!cashText) yield break;

        float t = 0f;
        while (t < moneyAnimDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moneyAnimDuration);
            long current = (long)Mathf.Lerp(from, to, k);

            // ✅ แสดงเป็น THB 1,234,567
            cashText.text = $"THB {current:N0}";
            yield return null;
        }

        // ✅ จบด้วยค่าเต็ม
        cashText.text = $"THB {to:N0}";
    }
}
