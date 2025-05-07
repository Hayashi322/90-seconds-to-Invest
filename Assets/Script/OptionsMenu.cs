using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public void OnOKButtonClicked()
    {
        SceneManager.LoadScene("MainMenu"); // หรือชื่อ scene ที่ใช้จริง
    }
}
