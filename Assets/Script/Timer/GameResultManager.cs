using System;
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

    // ตารางผลลัพธ์ที่จะซิงก์ถึงทุกคน
    public NetworkList<PlayerResultNet> results;

    // ผู้ชนะ (ไว้ใช้ในฉาก GameOver)
    public NetworkVariable<FixedString64Bytes> winnerName = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> winnerNetWorth = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        results = new NetworkList<PlayerResultNet>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        DontDestroyOnLoad(gameObject); // คงอยู่ข้ามฉาก
    }

    // ========== ขั้นที่ 1: Server คำนวณแล้วโหลด "Results" ==========
    public void CollectAndOpenResultsServer()
    {
        if (!IsServer) return;

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

        // โหลดฉากสรุปผลพร้อมกัน
        NetworkManager.SceneManager.LoadScene(resultsSceneName, LoadSceneMode.Single);
    }

    // ========== ขั้นที่ 2: จากฉาก Results → ไป GameOver ==========
    [ServerRpc(RequireOwnership = false)]
    public void ProceedToGameOverServerRpc()
    {
        if (!IsServer) return;
        NetworkManager.SceneManager.LoadScene(gameOverSceneName, LoadSceneMode.Single);
    }
}
