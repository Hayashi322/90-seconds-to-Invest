using UnityEngine;
using UnityEngine.SceneManagement;

public class WinSceneClickToContinue : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene("FinalScoreScene");
        }
    }
}
