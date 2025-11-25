using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public static class LeaderboardSubmitter
{
    private const string LeaderboardId = "top_networth";

    /// <summary>
    /// ส่งคะแนนขึ้น Leaderboard
    /// </summary>
    public static async Task SubmitScoreAsync(float score, string playerName)
    {
        try
        {
            // ให้ wrapper จัดการ Initialize + Sign-in ทั้งหมด
            await AuthenticationWrapper.DoAuth();

            var options = new AddPlayerScoreOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "name", playerName }
                }
            };

            var entry = await LeaderboardsService.Instance.AddPlayerScoreAsync(
                LeaderboardId,
                score,
                options
            );

            Debug.Log($"[LB] SubmitScore OK. rank={entry.Rank}, score={entry.Score}, name={playerName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LB] SubmitScoreAsync failed: {e}");
        }
    }

    /// <summary>
    /// ดึง Top N คะแนนไปใช้ที่หน้า LeaderboardScreen
    /// </summary>
    public static async Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(int limit = 10)
    {
        await AuthenticationWrapper.DoAuth();

        var response = await LeaderboardsService.Instance.GetScoresAsync(
            LeaderboardId,
            new GetScoresOptions { Limit = limit }
        );

        return response.Results;
    }
}
