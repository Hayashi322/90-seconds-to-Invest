using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Leaderboards;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI leaderboardText;
    private const string LeaderboardId = "top_networth";

    private async void OnEnable()
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (leaderboardText == null) return;

        leaderboardText.text = "Loading...";

        try
        {
            // ดึงอันดับ 1–10
            var scores = await LeaderboardsService.Instance.GetScoresAsync(
                LeaderboardId,
                new GetScoresOptions { Limit = 10 }
            );

            var sb = new StringBuilder();
            sb.AppendLine("อันดับ | ชื่อ | เงินสุทธิ");

            int rank = 1;
            foreach (var entry in scores.Results)
            {
                // ถ้าอยากใช้ชื่อในเกม ให้เก็บใน PlayerName อื่น ๆ ร่วมด้วย
                string name = string.IsNullOrEmpty(entry.Metadata)
                    ? $"Player {rank}"
                    : entry.Metadata;   // (ออปชัน ถ้าเธอใช้ metadata เก็บชื่อ)

                sb.AppendLine($"{rank}. {name} - {entry.Score:N0}");
                rank++;
            }

            leaderboardText.text = sb.ToString();
        }
        catch (System.Exception e)
        {
            leaderboardText.text = "โหลดอันดับไม่สำเร็จ";
            Debug.LogError($"[Leaderboard] Load failed : {e}");
        }
    }
}
