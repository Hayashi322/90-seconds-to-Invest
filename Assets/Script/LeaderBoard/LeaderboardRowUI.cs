using TMPro;
using UnityEngine;

public class LeaderboardRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(int rank, string name, double score)
    {
        if (rankText) rankText.text = rank.ToString();
        if (nameText) nameText.text = name;
        if (scoreText) scoreText.text = score.ToString("N0");
    }
}
