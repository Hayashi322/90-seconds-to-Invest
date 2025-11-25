using UnityEngine;

public class OptionsMenu : MonoBehaviour
{
    public void OnOKButtonClicked()
    {
        // ใช้ helper ตัวเดียว
        NetworkReturnToMenu.ReturnToMenu();
    }
}
