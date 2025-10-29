using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct PlayerResultNet : INetworkSerializable, IEquatable<PlayerResultNet>
{
    public ulong clientId;
    public FixedString64Bytes playerName;
    public float netWorth;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref clientId);
        s.SerializeValue(ref playerName);
        s.SerializeValue(ref netWorth);
    }

    public bool Equals(PlayerResultNet other) =>
        clientId == other.clientId && playerName.Equals(other.playerName) && Mathf.Approximately(netWorth, other.netWorth);
}

public class GameResultManager : NetworkBehaviour
{
    public static GameResultManager Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string resultsSceneName = "Results";
    [SerializeField] private string gameOverSceneName = "GameOver";

    [Header("Waiting Rule (รอ client ให้พร้อม)")]
    [SerializeField] private int minConnectedPlayers = 2;      // โฮสต์ + ไคลเอนต์ >= 1
    [SerializeField] private float waitForClientsTimeout = 15f; // วินาที

    // ต้อง new ตั้งแต่ตอนประกาศ (ตามข้อกำหนดของ NGO)
    public NetworkList<PlayerResultNet> results = new NetworkList<PlayerResultNet>();

    public NetworkVariable<FixedString64Bytes> winnerName =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> winnerNetWorth =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // guards ภายใน
    private bool isWaitingClients = false;
    private bool isSceneLoading = false; // กันสั่ง LoadScene ซ้ำ

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        DontDestroyOnLoad(gameObject);

        // ฟังตอนโหลดเสร็จเพื่อ reset แฟล็ก
        if (NetworkManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadCompleted;
    }

    private void OnDestroy()
    {
        if (NetworkManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= OnLoadCompleted;
    }

    private void OnLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> ok, List<ulong> timeout)
    {
        if (sceneName == resultsSceneName || sceneName == gameOverSceneName)
            isSceneLoading = false;
    }

    // ========== (ยังคงไว้ เผื่ออยากไป Results) ==========
    public void CollectAndOpenResultsServer()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;
        if (isSceneLoading) return;

        int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int need = Mathf.Max(1, minConnectedPlayers);

        if (connected < need)
        {
            if (!isWaitingClients)
            {
                isWaitingClients = true;
                Debug.Log($"[Results] Waiting for clients... {connected}/{need}. Timeout = {waitForClientsTimeout:F1}s");
                StartCoroutine(WaitForClientsThenOpen());
            }
            return;
        }

        DoCollectResultsAndLoadScene();
    }

    private IEnumerator WaitForClientsThenOpen()
    {
        float start = Time.unscaledTime;
        int need = Mathf.Max(1, minConnectedPlayers);

        while (NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsServer &&
               NetworkManager.Singleton.ConnectedClientsIds.Count < need &&
               (Time.unscaledTime - start) < waitForClientsTimeout)
        {
            yield return null;
        }

        isWaitingClients = false;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
            Debug.Log($"[Results] Proceeding with {connected} connected player(s).");
            DoCollectResultsAndLoadScene();
        }
    }

    private void DoCollectResultsAndLoadScene()
    {
        if (isSceneLoading) return;
        isSceneLoading = true;

        results.Clear();

        // ดึงราคาอ้างอิง
        var market = StockMarketManager.Instance;
        Dictionary<string, float> priceByName = new();
        if (market && market.networkStocks != null)
        {
            for (int i = 0; i < market.networkStocks.Count; i++)
            {
                var s = market.networkStocks[i];
                priceByName[s.stockName.ToString()] = s.currentPrice;
            }
        }

        // ราคาทอง (fallback)
        float goldUnit = 48_000f;
        if (GoldShopManager.Instance) goldUnit = GoldShopManager.Instance.SellGoldPrice.Value;

        // วนทุกผู้เล่น
        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var pObj = kv.Value.PlayerObject;
            if (!pObj) continue;

            string pname = $"Player {kv.Key}";
            var inv = pObj.GetComponent<InventoryManager>();
            if (inv == null) continue;

            float worth = inv.cash.Value;

            // หุ้น
            if (inv.stockHoldings != null)
            {
                for (int i = 0; i < inv.stockHoldings.Count; i++)
                {
                    var h = inv.stockHoldings[i];
                    var name = h.stockName.ToString();
                    if (priceByName.TryGetValue(name, out var px))
                        worth += h.quantity * px;
                }
            }

            // ทอง
            worth += inv.goldAmount.Value * goldUnit;

            results.Add(new PlayerResultNet
            {
                clientId = kv.Key,
                playerName = (FixedString64Bytes)pname,
                netWorth = worth
            });
        }

        // หาแชมป์
        if (results.Count > 0)
        {
            PlayerResultNet top = results[0];
            for (int i = 1; i < results.Count; i++)
                if (results[i].netWorth > top.netWorth) top = results[i];

            winnerName.Value = top.playerName;
            winnerNetWorth.Value = top.netWorth;
        }
        else
        {
            winnerName.Value = "No Player";
            winnerNetWorth.Value = 0;
        }

        NetworkManager.SceneManager.LoadScene(resultsSceneName, LoadSceneMode.Single);
    }

    // ========== ทำลาย Player ทั้งหมดก่อนย้ายฉาก ==========
    private void DespawnAllPlayersServer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        foreach (var kv in nm.ConnectedClients)
        {
            var playerObj = kv.Value.PlayerObject;
            if (!playerObj) continue;

            var no = playerObj.GetComponent<NetworkObject>();
            if (no && no.IsSpawned)
            {
                // true = แจ้งทุกเครื่องและ Destroy GameObject ให้ด้วย
                no.Despawn(true);
            }
        }
    }

    // ========== ไป GameOver (ลบผู้เล่นก่อน) ==========
    [ServerRpc(RequireOwnership = false)]
    public void ProceedToGameOverServerRpc()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;
        if (isSceneLoading) return;

        isSceneLoading = true;

        // 1) ลบผู้เล่นออกจากเน็ตเวิร์กก่อน
        DespawnAllPlayersServer();

        // 2) รอ 1 เฟรมกันแข่งกับ despawn แล้วค่อยย้ายฉากแบบซิงก์
        StartCoroutine(LoadGameOverNextFrame());
    }

    private IEnumerator LoadGameOverNextFrame()
    {
        yield return null;
        NetworkManager.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
    }
}
