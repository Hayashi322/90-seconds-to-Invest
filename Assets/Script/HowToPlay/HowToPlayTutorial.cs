using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class HowToPlayTutorial : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image tutorialImage;              // รูปสอนเล่น
    [SerializeField] private TextMeshProUGUI descriptionText;  // ข้อความอธิบาย
    [SerializeField] private Button prevButton;                // ปุ่มย้อนกลับ
    [SerializeField] private Button nextButton;                // ปุ่มถัดไป
    [SerializeField] private Button backToMenuButton;          // ปุ่มกลับเมนู

    [Header("Pages Data")]
    [SerializeField] private Sprite[] tutorialImages;          // รูปแต่ละหน้า
    [TextArea(2, 5)]
    [SerializeField] private string[] descriptions;            // ข้อความแต่ละหน้า

    [Header("Menu Scene")]
    [SerializeField] private string menuSceneName = "MainMenu"; // ชื่อ Scene เมนูหลัก

    private int currentPage = 0;

    private void Start()
    {
        // ผูกปุ่ม
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(BackToMenu);

        UpdatePage();
    }

    private void UpdatePage()
    {
        if (tutorialImages.Length == 0 || descriptions.Length == 0)
        {
            Debug.LogWarning("ยังไม่ได้เซ็ตรูปหรือคำอธิบายใน HowToPlayTutorial");
            return;
        }

        // อัปเดตรูป
        if (tutorialImage != null)
            tutorialImage.sprite = tutorialImages[currentPage];

        // อัปเดตข้อความ
        if (descriptionText != null)
            descriptionText.text = descriptions[currentPage];
    }

    public void NextPage()
    {
        // วนลูปจากหน้าสุดท้าย → หน้าแรก
        currentPage++;
        if (currentPage >= tutorialImages.Length)
            currentPage = 0;

        UpdatePage();
    }

    public void PrevPage()
    {
        // วนลูปจากหน้าแรก → หน้าสุดท้าย
        currentPage--;
        if (currentPage < 0)
            currentPage = tutorialImages.Length - 1;

        UpdatePage();
    }

    public void BackToMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }
}
