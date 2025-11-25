using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLeaderboardButton : MonoBehaviour
{
    [SerializeField] private string leaderboardSceneName = "LeaderboardScene";

    // ผูกกับปุ่มใน Inspector
    public void OnClickOpenLeaderboard()
    {
        SceneManager.LoadScene(leaderboardSceneName);
    }
}
