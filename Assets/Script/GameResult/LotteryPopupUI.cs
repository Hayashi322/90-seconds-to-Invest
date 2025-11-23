using System.Collections;
using TMPro;
using UnityEngine;

public class LotteryPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup rootCanvasGroup;

    [Header("Winning Number (รางวัลที่ 1)")]
    [SerializeField] private TMP_Text[] winningDigitTexts;   // 6 หลักด้านบน

    [Header("Player Ticket UI (ใบหวยของเรา)")]
    [SerializeField] private GameObject playerTicketRoot;    // กล่องรูปบัตรหวยของผู้เล่น
    [SerializeField] private TMP_Text playerTicketText;      // Text ตัวเลข 6 หลักบนบัตรของผู้เล่น (1 ชิ้นพอ)

    [Header("Result Text")]
    [SerializeField] private TMP_Text resultText;            // ข้อความถูกหวย / หวยกิน / ไม่ได้ซื้อ

    [Header("Anim Settings")]
    [SerializeField] private float fadeDuration = 0.25f;     // เวลาเฟดเข้า / ออก
    [SerializeField] private float spinStepDelay = 0.06f;    // ดีเลย์ของแต่ละ "สปิน"
    [SerializeField] private int spinCountPerDigit = 10;     // สุ่มกี่ครั้งต่อ 1 หลัก
    [SerializeField] private float digitRevealDelay = 0.2f;  // เวลาหน่วงระหว่างเฉลยแต่ละหลัก
    [SerializeField] private float afterResultDelay = 5.0f;  // ค้างให้ดูผลก่อนปิด

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// ticketNumber < 0  = ผู้เล่นคนนี้ "ไม่มีหวย"
    /// winningNumber     = เลขรางวัลที่ 1 (ทุกคนใช้เลขเดียวกัน)
    /// prize             = เงินรางวัลที่ได้ (0 ถ้าไม่ถูกรางวัล)
    /// </summary>
    public IEnumerator ShowTicketAndResult(int ticketNumber, int winningNumber, long prize)
    {
        bool hasTicket = ticketNumber >= 0;

        // แปลงเลขเป็น string 6 หลัก
        string winningStr = winningNumber.ToString("000000");
        string ticketStr = hasTicket ? ticketNumber.ToString("000000") : "-";

        gameObject.SetActive(true);

        // ---------- รีเซ็ต UI เริ่มต้น ----------
        if (rootCanvasGroup)
            rootCanvasGroup.alpha = 0f;

        if (resultText)
            resultText.text = "";

        // เลขรางวัลตั้งต้นเป็น 0 ทั้งหมด
        if (winningDigitTexts != null)
        {
            foreach (var txt in winningDigitTexts)
            {
                if (txt) txt.text = "0";
            }
        }

        // ใบหวยของผู้เล่น (ไม่หมุน แสดงเลขจริงเลย)
        if (playerTicketRoot)
            playerTicketRoot.SetActive(hasTicket);

        if (hasTicket && playerTicketText)
        {
            // ตัวเลข 6 หลักที่บัตรของเรา
            playerTicketText.text = ticketStr;
        }
        else if (playerTicketText)
        {
            playerTicketText.text = "";
        }

        // ---------- Fade In ----------
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (rootCanvasGroup)
                rootCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        // ---------- หมุนเลข "รางวัลที่ 1" ทีละหลักจากหลัง → หน้า ----------
        if (winningDigitTexts != null)
        {
            // index 5 = หลักหน่วย, 0 = หลักแสน
            for (int i = 5; i >= 0; i--)
            {
                if (i >= winningDigitTexts.Length) continue;
                var digitText = winningDigitTexts[i];
                if (!digitText) continue;

                // สปินสุ่มเลขก่อน
                for (int spin = 0; spin < spinCountPerDigit; spin++)
                {
                    digitText.text = Random.Range(0, 10).ToString();
                    yield return new WaitForSeconds(spinStepDelay);
                }

                // เฉลยเลขจริงของรางวัลที่ 1
                if (i < winningStr.Length)
                    digitText.text = winningStr[i].ToString();
                else
                    digitText.text = "0";

                yield return new WaitForSeconds(digitRevealDelay);
            }
        }

        // ---------- สรุปผล ----------
        bool win = hasTicket && (ticketNumber == winningNumber) && prize > 0;

        if (!hasTicket)
        {
            if (resultText)
                resultText.text = "คุณไม่ได้ซื้อหวยในรอบนี้";
        }
        else if (win)
        {
            if (resultText)
                resultText.text = $"ยินดีด้วย! คุณถูกรางวัล {prize:N0} บาท";
        }
        else
        {
            if (resultText)
                resultText.text = "เสียใจด้วย คุณถูกหวยกิน...";
        }

        // ค้างให้ดูผล
        yield return new WaitForSeconds(afterResultDelay);

        // ---------- Fade Out ----------
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (rootCanvasGroup)
                rootCanvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
