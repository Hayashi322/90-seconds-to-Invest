using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene"); // เปลี่ยนชื่อซีนตามที่เธอใช้จริง
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("SettingsScene"); // หรือใช้ Panel ก็ได้ ถ้าอยู่ในหน้าเดียวกัน
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting...");
    }
}