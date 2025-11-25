using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public static class LeaderboardSubmitter
{
    // ใช้ ID เดียวกับที่ตั้งใน Dashboard
    private const string LeaderboardId = "top_networth";

    private static bool _initialized = false;

    /// <summary>
    /// เตรียม Unity Services + Anonymous Auth
    /// </summary>
    private static async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized ||
                UnityServices.State == ServicesInitializationState.Initializing)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[LB] Signed in. PlayerId = {AuthenticationService.Instance.PlayerId}");
            }

            _initialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LB] Initialize failed: {e}");
        }
    }

    /// <summary>
    /// ส่งคะแนนขึ้น Leaderboard พร้อมตั้งชื่อผู้เล่นเป็นชื่อในเกม
    /// </summary>
    public static async Task SubmitScoreAsync(float score, string playerName)
    {
        await EnsureInitializedAsync();

        try
        {
            // 1) อัปเดตชื่อโปรไฟล์ของ UGS ให้ตรงกับชื่อในเกม
            if (!string.IsNullOrWhiteSpace(playerName)
                && AuthenticationService.Instance.IsSignedIn)
            {
                try
                {
                    await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
                    Debug.Log($"[LB] UpdatePlayerNameAsync -> {playerName}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LB] UpdatePlayerName failed: {ex.Message}");
                }
            }

            // 2) ส่งคะแนน (จะผูกกับ PlayerName ด้านบนอัตโนมัติ)
            var options = new AddPlayerScoreOptions
            {
                // ถ้าอยากเก็บ metadata เพิ่มก็ใส่ได้
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

            Debug.Log($"[LB] SubmitScore OK. rank={entry.Rank}, score={entry.Score}, name={entry.PlayerName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LB] SubmitScore failed: {e}");
        }
    }

    /// <summary>
    /// ดึง Top N ไว้ใช้หน้า Leaderboard UI
    /// </summary>
    public static async Task<IReadOnlyList<LeaderboardEntry>> GetTopScoresAsync(int limit = 10)
    {
        await EnsureInitializedAsync();

        var response = await LeaderboardsService.Instance.GetScoresAsync(
            LeaderboardId,
            new GetScoresOptions { Limit = limit }
        );

        return response.Results;
    }
}
