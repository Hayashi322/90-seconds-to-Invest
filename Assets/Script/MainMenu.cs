using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // เพิ่มสำหรับ TextMeshPro

public class MainMenu : MonoBehaviour
{
    public TMP_InputField nameInputField;

    public void PlayGame()
    {
        string inputName = nameInputField.text;

        if (string.IsNullOrEmpty(inputName))
        {
            Debug.Log("กรุณาใส่ชื่อก่อนเริ่มเกม");
            return;
        }

        PlayerData.Instance.playerName = inputName;
        SceneManager.LoadScene("LobbyScene");
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene("SettingsScene");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting...");
    }
}
