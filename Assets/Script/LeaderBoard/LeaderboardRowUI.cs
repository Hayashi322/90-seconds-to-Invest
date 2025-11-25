using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(int rank, string playerName, double score)
    {
        if (rankText) rankText.text = $"#{rank}";
        if (nameText) nameText.text = playerName;
        if (scoreText) scoreText.text = $"{score:N0}";   // 1,234,567 แบบไม่มีทศนิยม
    }
}
