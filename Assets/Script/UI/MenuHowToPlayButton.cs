using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuHowToPlayButton : MonoBehaviour
{
    // ใส่ชื่อ Scene วิธีเล่นตรงนี้ให้ตรงกับใน Build Settings
    [SerializeField] private string howToPlaySceneName = "HowToPlayScene";

    // เรียกใช้ในปุ่ม OnClick
    public void GoToHowToPlay()
    {
        SceneManager.LoadScene(howToPlaySceneName);
    }
}
