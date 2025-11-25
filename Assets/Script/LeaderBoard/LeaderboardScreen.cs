using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Leaderboards.Models;

public class LeaderboardScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform rowsParent;           // Content ที่จะวางแถว
    [SerializeField] private LeaderboardRowUI rowPrefab;     // Prefab ของ 1 แถว
    [SerializeField] private TextMeshProUGUI statusText;     // ข้อความสถานะ
    [SerializeField] private Button refreshButton;           // ปุ่ม Reload
    [SerializeField] private Button backButton;              // ปุ่มกลับเมนู

    [Header("Settings")]
    [SerializeField] private int maxEntries = 10;

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

        await RefreshLeaderboardAsync();
    }

    private async void OnClickRefresh()
    {
        await RefreshLeaderboardAsync();
    }

    private void OnClickBack()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private async Task RefreshLeaderboardAsync()
    {
        if (statusText) statusText.text = "กำลังโหลดอันดับ...";

        // ลบแถวเก่า
        foreach (var row in _spawnedRows)
        {
            if (row) Destroy(row.gameObject);
        }
        _spawnedRows.Clear();

        try
        {
            // ดึง Top N จาก helper (ข้างในมี Initialize + SignIn อยู่แล้ว)
            IReadOnlyList<LeaderboardEntry> entries =
                await LeaderboardSubmitter.GetTopScoresAsync(maxEntries);

            Debug.Log($"[LB UI] entries count = {entries.Count}");

            int rank = 1;
            foreach (var entry in entries)
            {
                // ---------- เลือกชื่อที่จะโชว์ ----------
                string name = entry.PlayerId; // fallback ค่าเริ่มต้นเป็น PlayerId

                // 1) ลองอ่านจาก metadata JSON {"name":"PlayerName"}
                string metaName = ExtractNameFromMetadata(entry.Metadata);
                if (!string.IsNullOrEmpty(metaName))
                {
                    name = metaName;
                }
                // 2) ถ้า metadata ไม่มี name ให้ใช้ PlayerName ของ UGS
                else if (!string.IsNullOrEmpty(entry.PlayerName))
                {
                    name = entry.PlayerName;
                }

                double score = entry.Score;

                var row = Instantiate(rowPrefab, rowsParent);
                row.Setup(rank, name, score);
                _spawnedRows.Add(row);

                Debug.Log($"[LB UI] rank={rank} id={entry.PlayerId} name={name} meta={entry.Metadata}");

                rank++;
            }

            if (_spawnedRows.Count == 0)
            {
                if (statusText) statusText.text = "ยังไม่มีใครติดอันดับ";
            }
            else
            {
                if (statusText) statusText.text = $"10 อันดับ นักลงทุน";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LB UI] RefreshLeaderboardAsync failed: {e}");
            if (statusText) statusText.text = "โหลดอันดับไม่สำเร็จ";
        }
    }

    /// <summary>
    /// ดึงค่า "name" ออกจาก metadata JSON แบบง่าย ๆ
    /// ตัวอย่าง metadata: {"name":"Player123"}
    /// </summary>
    private string ExtractNameFromMetadata(string metadataJson)
    {
        if (string.IsNullOrEmpty(metadataJson))
            return null;

        const string key = "\"name\":\"";
        int start = metadataJson.IndexOf(key, System.StringComparison.Ordinal);
        if (start < 0) return null;

        start += key.Length;
        int end = metadataJson.IndexOf('"', start);
        if (end < 0) return null;

        return metadataJson.Substring(start, end - start);
    }
}
