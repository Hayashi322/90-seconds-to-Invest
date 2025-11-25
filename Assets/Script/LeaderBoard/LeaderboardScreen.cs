using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ใช้ type LeaderboardEntry
using Unity.Services.Leaderboards.Models;

public class LeaderboardScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform rowsParent;           // Content ที่จะวางแถว
    [SerializeField] private LeaderboardRowUI rowPrefab;     // prefab แถวอันดับ
    [SerializeField] private TextMeshProUGUI statusText;     // ข้อความสถานะ เช่น "Loading..."
    [SerializeField] private Button refreshButton;           // ปุ่มโหลดใหม่
    [SerializeField] private Button backButton;              // ปุ่มกลับเมนู

    [Header("Settings")]
    [SerializeField] private int maxEntries = 10;            // แสดงกี่อันดับ

    private readonly List<LeaderboardRowUI> _spawnedRows = new();

    private async void Start()
    {
        if (statusText) statusText.text = "กำลังโหลดอันดับ...";

        if (refreshButton)
        {
            refreshButton.onClick.RemoveAllListeners();
            refreshButton.onClick.AddListener(OnClickRefresh);
        }

        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnClickBack);
        }

        // โหลดอันดับรอบแรก
        await RefreshLeaderboardAsync();
    }

    private async void OnClickRefresh()
    {
        await RefreshLeaderboardAsync();
    }

    private void OnClickBack()
    {
        // กลับหน้าเมนูหลัก
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private async Task RefreshLeaderboardAsync()
    {
        if (statusText) statusText.text = "กำลังโหลดอันดับ...";

        // ลบแถวเก่า ๆ ใน ScrollView
        foreach (var row in _spawnedRows)
        {
            if (row) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();

        try
        {
            // ใช้ helper ตัวเดียวกับฝั่งส่งคะแนน (จะจัดการ Initialize + SignIn ให้เอง)
            IReadOnlyList<LeaderboardEntry> entries =
                await LeaderboardSubmitter.GetTopScoresAsync(maxEntries);

            int rank = 1;
            foreach (var entry in entries)
            {
                // เลือกชื่อที่จะโชว์
                string name;

                if (!string.IsNullOrEmpty(entry.PlayerName))
                {
                    // ถ้ามี PlayerName จาก UGS ก็ใช้เลย
                    name = entry.PlayerName;
                }
                else
                {
                    // ไม่มีชื่อ ก็ fallback เป็น PlayerId
                    name = entry.PlayerId;
                }

                double score = entry.Score;

                // สร้างแถวใหม่ใน ScrollView
                var row = Instantiate(rowPrefab, rowsParent);
                row.Setup(rank, name, score);
                _spawnedRows.Add(row);

                rank++;
            }

            if (_spawnedRows.Count == 0)
            {
                if (statusText) statusText.text = "ยังไม่มีใครขึ้นอันดับเลย";
            }
            else
            {
                if (statusText) statusText.text = $"แสดง Top {_spawnedRows.Count} อันดับ";
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            if (statusText) statusText.text = "โหลดอันดับไม่สำเร็จ : " + e.Message;
        }
    }
}
