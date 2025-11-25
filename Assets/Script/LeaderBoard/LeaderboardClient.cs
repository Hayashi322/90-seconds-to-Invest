using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Authentication;

public static class LeaderboardClient
{
    // id ของ leaderboard บนเว็บ
    private const string LeaderboardId = "top_networth";

    /// <summary>
    /// ส่งค่า netWorth ของผู้เล่นขึ้น leaderboard
    /// </summary>
    public static async Task SubmitScoreAsync(long netWorth)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Leaderboard] Not signed in, skip submit.");
            return;
        }

        try
        {
            var result = await LeaderboardsService.Instance
                .AddPlayerScoreAsync(LeaderboardId, netWorth);

            Debug.Log($"[Leaderboard] Submit success. Rank={result.Rank}, Score={result.Score}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Leaderboard] Submit failed : {e}");
        }
    }
}
