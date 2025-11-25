using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameResultManager : NetworkBehaviour
{
    public static GameResultManager Instance { get; private set; }

    // เก็บผลล่าสุดแบบข้าม Scene
    public static readonly List<PlayerFinalResult> LastResults = new List<PlayerFinalResult>();

    // เลขรางวัลที่ 1 รอบล่าสุด
    public static int LastWinningNumber { get; private set; } = -1;

    [Header("Scene Names")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Lottery Prize")]
    [SerializeField] private int lotteryPrize = 6000000;

    [System.Serializable]
    public struct PlayerFinalResult
    {
        public ulong clientId;
        public string playerName;
        public int characterIndex;

        public double cash;
        public bool hasLottery;
        public int lotteryNumber;
        public bool lotteryWin;

        public double finalNetworth;
    }

    public struct PlayerFinalResultNet : INetworkSerializable
    {
        public ulong clientId;
        public FixedString64Bytes playerName;
        public int characterIndex;

        public double cash;
        public bool hasLottery;
        public int lotteryNumber;
        public bool lotteryWin;

        public double finalNetworth;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref characterIndex);
            serializer.SerializeValue(ref cash);
            serializer.SerializeValue(ref hasLottery);
            serializer.SerializeValue(ref lotteryNumber);
            serializer.SerializeValue(ref lotteryWin);
            serializer.SerializeValue(ref finalNetworth);
        }
    }

    // ใช้เฉพาะฝั่ง Server
    private readonly Dictionary<ulong, PlayerFinalResult> finalResults =
        new Dictionary<ulong, PlayerFinalResult>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // เรียกจาก Timer (ฝั่ง Server เท่านั้น)
    public void RequestGameOver()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[GRM] RequestGameOver() ignored (not server)");
            return;
        }

        Debug.Log("[GRM] RequestGameOver() by server");
        ProceedToGameOver();
    }

    private void ProceedToGameOver()
    {
        if (!IsServer) return;

        Debug.Log("[GRM] ProceedToGameOver()");

        BuildResultsAndBroadcast();
        LoadGameOverScene();
    }

    private void BuildResultsAndBroadcast()
    {
        finalResults.Clear();

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[GRM] NetworkManager.Singleton is null");
            return;
        }

        // ดึงเลขรางวัลที่ 1
        if (LotteryManager.Instance != null)
        {
            LastWinningNumber = LotteryManager.Instance.WinningTicketNumber;
            Debug.Log($"[GRM] Winning ticket this game = {LastWinningNumber:000000}");
        }
        else
        {
            LastWinningNumber = -1;
            Debug.LogWarning("[GRM] LotteryManager.Instance == null → LastWinningNumber = -1");
        }

        foreach (var entry in nm.ConnectedClients)
        {
            ulong clientId = entry.Key;
            var playerObj = entry.Value.PlayerObject;
            if (!playerObj)
            {
                Debug.LogWarning($"[GRM] PlayerObject missing for {clientId}");
                continue;
            }

            var inv = playerObj.GetComponent<InventoryManager>();
            var lotto = playerObj.GetComponent<PlayerLotteryState>();
            var hero = playerObj.GetComponent<HeroControllerNet>();

            string name = LobbyManager.GetCachedPlayerName(clientId) ?? $"P{clientId}";

            int characterIndex = hero ? hero.CharacterIndex.Value : -1;
            double cash = inv ? inv.cash.Value : 0f;
            bool hasTicket = lotto && lotto.HasTicket.Value;
            int ticketNumber = lotto ? lotto.TicketNumber.Value : -1;
            bool lottoWin = false;

            if (hasTicket && LotteryManager.Instance != null &&
                LotteryManager.Instance.HasWinningTicket(ticketNumber))
            {
                lottoWin = true;
                cash += lotteryPrize;
            }

            var result = new PlayerFinalResult
            {
                clientId = clientId,
                playerName = name,
                characterIndex = characterIndex,
                cash = cash,
                hasLottery = hasTicket,
                lotteryNumber = ticketNumber,
                lotteryWin = lottoWin,
                finalNetworth = cash
            };

            finalResults[clientId] = result;

            Debug.Log($"[GRM] {name} | char={characterIndex} | cash={cash} " +
                      $"| ticket={ticketNumber} | lottoWin={lottoWin}");
        }

        Debug.Log($"[GRM] BuildResults done, count={finalResults.Count}");

        // 1) เก็บไว้ใน static ให้ทุก scene ใช้
        LastResults.Clear();
        foreach (var kvp in finalResults)
            LastResults.Add(kvp.Value);

        // 2) ส่งให้ทุก client
        var netList = new List<PlayerFinalResultNet>();
        foreach (var kvp in finalResults)
        {
            var fr = kvp.Value;
            var net = new PlayerFinalResultNet
            {
                clientId = fr.clientId,
                playerName = fr.playerName,
                characterIndex = fr.characterIndex,
                cash = fr.cash,
                hasLottery = fr.hasLottery,
                lotteryNumber = fr.lotteryNumber,
                lotteryWin = fr.lotteryWin,
                finalNetworth = fr.finalNetworth
            };
            netList.Add(net);
        }

        ReceiveResultsClientRpc(netList.ToArray(), LastWinningNumber);
    }

    [ClientRpc]
    private void ReceiveResultsClientRpc(PlayerFinalResultNet[] results, int winningNumber)
    {
        LastWinningNumber = winningNumber;
        Debug.Log($"[GRM] ReceiveResultsClientRpc: WinningNumber={LastWinningNumber:000000}");

        LastResults.Clear();

        foreach (var r in results)
        {
            LastResults.Add(new PlayerFinalResult
            {
                clientId = r.clientId,
                playerName = r.playerName.ToString(),
                characterIndex = r.characterIndex,
                cash = r.cash,
                hasLottery = r.hasLottery,
                lotteryNumber = r.lotteryNumber,
                lotteryWin = r.lotteryWin,
                finalNetworth = r.finalNetworth
            });
        }

        Debug.Log($"[GRM] ReceiveResultsClientRpc: count={LastResults.Count}");

        // ส่งสกอร์ของ client เครื่องนี้ขึ้น Leaderboard
        SubmitLocalPlayerToLeaderboard();
    }

    /// <summary>
    /// หา result ของ client เครื่องนี้ แล้วส่งขึ้น Unity Cloud Leaderboards
    /// </summary>
    private async void SubmitLocalPlayerToLeaderboard()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogWarning("[GRM] Cannot submit leaderboard: NetworkManager is null.");
            return;
        }

        ulong localId = nm.LocalClientId;

        bool found = false;
        PlayerFinalResult myResult = default;

        foreach (var r in LastResults)
        {
            if (r.clientId == localId)
            {
                myResult = r;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[GRM] Local result not found for leaderboard submit.");
            return;
        }

        string playerName = string.IsNullOrEmpty(myResult.playerName)
            ? $"P{localId}"
            : myResult.playerName;

        float score = (float)myResult.finalNetworth;

        await LeaderboardSubmitter.SubmitScoreAsync(score, playerName);
        Debug.Log($"[GRM] Leaderboard submit: {playerName} score={score}");
    }

    private void LoadGameOverScene()
    {
        var nm = NetworkManager;

        if (nm != null && nm.IsListening)
        {
            nm.SceneManager.LoadScene(
                gameOverSceneName,
                LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
    }

    public static void ResetStatics()
    {
        LastResults.Clear();
        LastWinningNumber = -1;
    }
}
