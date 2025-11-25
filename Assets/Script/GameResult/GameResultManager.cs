using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class GameResultManager : NetworkBehaviour
{
    public static GameResultManager Instance { get; private set; }

    // เก็บผลล่าสุดแบบข้าม Scene (static อยู่ได้ทุกเครื่อง)
    public static readonly List<PlayerFinalResult> LastResults = new List<PlayerFinalResult>();

    // ✅ เลขรางวัลที่ 1 รอบล่าสุด (ใช้ใน GameOverResultController + LotteryPopup)
    public static int LastWinningNumber { get; private set; } = -1;

    [Header("Scene Names")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Lottery Prize")]
    [SerializeField] private int lotteryPrize = 6_000_000;

    // ========================
    // Struct ฝั่ง Gameplay (ใช้ในโค้ดปกติ)
    // ========================
    [System.Serializable]
    public struct PlayerFinalResult
    {
        public ulong clientId;
        public string playerName;
        public int characterIndex;

        public float cash;          // เงินหลัง + หวยแล้ว
        public bool hasLottery;
        public int lotteryNumber;
        public bool lotteryWin;

        public float finalNetworth; // เอาไว้จัดอันดับ (ตอนนี้ = cash)
    }

    // ========================
    // Struct สำหรับส่งผ่าน RPC (ต้อง INetworkSerializable)
    // ========================
    public struct PlayerFinalResultNet : INetworkSerializable
    {
        public ulong clientId;
        public FixedString64Bytes playerName;
        public int characterIndex;

        public float cash;
        public bool hasLottery;
        public int lotteryNumber;
        public bool lotteryWin;

        public float finalNetworth;

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
        // เป็น Scene Object ของ GameSceneNet พอ ไม่ต้อง DontDestroyOnLoad
    }

    // ========================
    // เรียกจาก Timer (ฝั่ง Server เท่านั้น)
    // ========================
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

        BuildResultsAndBroadcast();   // รวบรวม + ส่งให้ทุก client
        LoadGameOverScene();         // แล้วค่อยย้ายไป GameOver
    }

    // ========================
    // รวบรวมผลลัพธ์ + ส่ง RPC
    // ========================
    private void BuildResultsAndBroadcast()
    {
        finalResults.Clear();

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[GRM] NetworkManager.Singleton is null");
            return;
        }

        // ✅ ดึงเลขรางวัลที่ 1 จาก LotteryManager เสมอ
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

            // =========================
            // ✅ หาชื่อผู้เล่นจาก Lobby / PlayerData / PlayerPrefs
            // =========================
            string name = null;

            // 1) จาก LobbyManager cache (ทุกเครื่องใช้เหมือนกัน)
            name = LobbyManager.GetCachedPlayerName(clientId);

            // 2) ถ้าเป็น local player และยังไม่มีชื่อ ลองดึงจาก PlayerData / PlayerPrefs
            if (string.IsNullOrWhiteSpace(name) &&
                NetworkManager.Singleton != null &&
                NetworkManager.Singleton.LocalClientId == clientId)
            {
                if (PlayerData.Instance != null &&
                    !string.IsNullOrWhiteSpace(PlayerData.Instance.playerName))
                {
                    name = PlayerData.Instance.playerName;
                }
                else
                {
                    name = PlayerPrefs.GetString("player_name", "");
                }
            }

            // 3) fallback สุดท้าย
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"P{clientId}";
            }
            // =========================

            if (hero == null)
            {
                Debug.LogWarning($"[GRM] HeroControllerNet missing on {clientId}");
            }

            int characterIndex = hero ? hero.CharacterIndex.Value : -1;
            float cash = inv ? inv.cash.Value : 0f;
            bool hasTicket = lotto && lotto.HasTicket.Value;
            int ticketNumber = lotto ? lotto.TicketNumber.Value : -1;
            bool lottoWin = false;

            // เช็คหวย + บวกเงินรางวัลถ้าถูก
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

            Debug.Log($"[GRM] {name} | char={characterIndex} | cash={cash} | ticket={ticketNumber} | lottoWin={lottoWin}");
        }

        Debug.Log($"[GRM] BuildResults done, count={finalResults.Count}");

        // 1) เก็บสำเนาไว้ใน Host
        LastResults.Clear();
        foreach (var kvp in finalResults)
            LastResults.Add(kvp.Value);

        // 2) แปลงเป็น PlayerFinalResultNet[] เพื่อส่งให้ทุก client ผ่าน RPC
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

        // ✅ ส่งผล + เลขรางวัลที่ 1 ไปทุก client
        ReceiveResultsClientRpc(netList.ToArray(), LastWinningNumber);
    }

    // ========================
    // ClientRpc: รับผลมาใส่ LastResults (ทุกเครื่อง)
    // ========================
    [ClientRpc]
    private void ReceiveResultsClientRpc(PlayerFinalResultNet[] results, int winningNumber)
    {
        // ✅ sync เลขรางวัลที่ 1 มาทุก client ตรงนี้
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
    }

    // ========================
    // โหลด Scene GameOver
    // ========================
    private void LoadGameOverScene()
    {
        var nm = NetworkManager;
        if (nm != null && nm.IsListening)
        {
            nm.SceneManager.LoadScene(
                gameOverSceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameOverSceneName);
        }
    }
}
