using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneTransition : MonoBehaviour
{
    private bool hasPressedEnter = false;

    void Update()
    {
        if (!hasPressedEnter && Input.GetKeyDown(KeyCode.Return))
        {
            hasPressedEnter = true;
            LoadResultScene();
        }
    }

    void LoadResultScene()
    {
        int characterIndex = PlayerData.Instance.selectedCharacterIndex;

        // โหลด scene ตามตัวละครที่ชนะ (กำหนดชื่อ scene ไว้ตาม index)
        switch (characterIndex)
        {
            case 0:
                SceneManager.LoadScene("Win_Orange"); // คนผมส้ม
                break;
            case 1:
                SceneManager.LoadScene("Win_Brown"); // คนผมหางม้า
                break;
            case 2:
                SceneManager.LoadScene("Win_Gray"); // คนผมเทา
                break;
            default:
                Debug.LogError("ไม่พบ index ตัวละครที่ถูกเลือก");
                break;
        }
    }
}
