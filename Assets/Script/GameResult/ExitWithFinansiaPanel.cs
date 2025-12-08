using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitWithFinansiaPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject promoPanel;   // Panel ที่มีโลโก้ + ข้อความ
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private float showTime = 5f;     // แสดงกี่วินาที

    private bool isProcessing = false;

    private void Awake()
    {
        // ให้แน่ใจว่าเริ่มเกมมา Panel ถูกปิดไว้ก่อน
        if (promoPanel != null)
            promoPanel.SetActive(false);
    }

    /// <summary>
    /// ผูกฟังก์ชันนี้กับปุ่ม "ออก"
    /// </summary>
    public void OnExitButtonClicked()
    {
        if (isProcessing) return;       // กันคลิกรัว ๆ
        StartCoroutine(ShowAndExitRoutine());
    }

    private IEnumerator ShowAndExitRoutine()
    {
        isProcessing = true;

        // เปิด Panel โปรโมต
        if (promoPanel != null)
            promoPanel.SetActive(true);

        // รอ 5 วินาที (หรือ showTime)
        yield return new WaitForSeconds(showTime);

        // ไปหน้าเมนูเหมือนเดิม
        SceneManager.LoadScene(menuSceneName);
    }
}
