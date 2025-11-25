using System;
using System.Collections.Generic;   // สำคัญ
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    // 👇 cache ชื่อทุกคนแบบ static อยู่ได้ข้าม Scene (ทุกเครื่องจะมีเหมือนกัน)
    public static readonly Dictionary<ulong, string> CachedNames = new();

    [Header("Character Catalog (index ต้องตรงกับรูปใน UI)")]
    public string[] characterNames;

    [Serializable]
    public struct PlayerStateNet : INetworkSerializable, IEquatable<PlayerStateNet>
    {
        public ulong clientId;
        public FixedString32Bytes playerName;
        public int characterIndex; // -1 = ยังไม่เลือก
        public bool ready;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref characterIndex);
            serializer.SerializeValue(ref ready);
        }

        public bool Equals(PlayerStateNet other)
        {
            return clientId == other.clientId
                   && playerName.Equals(other.playerName)
                   && characterIndex == other.characterIndex
                   && ready == other.ready;
        }
    }

    public NetworkList<PlayerStateNet> players;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        if (players == null) players = new NetworkList<PlayerStateNet>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // --- ฝั่ง Client: ส่งชื่อที่เซฟไว้ขึ้น Server ---
        if (IsClient)
        {
            string savedName = null;

            // 1) ดึงจาก PlayerData ก่อน
            if (PlayerData.Instance != null &&
                !string.IsNullOrWhiteSpace(PlayerData.Instance.playerName))
            {
                savedName = PlayerData.Instance.playerName;
            }
            else
            {
                // 2) fallback ไปที่ PlayerPrefs
                savedName = PlayerPrefs.GetString("player_name", "");
            }

            if (!string.IsNullOrWhiteSpace(savedName))
            {
                SetNameServerRpc(savedName);
            }
        }

        // --- ฝั่ง Server: ลงทะเบียน callback ต่าง ๆ ---
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                OnClientConnected(clientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // ✅ ก่อนถูก Despawn ให้ก็อปชื่อทั้งหมดใส่ CachedNames บน "ทุกเครื่อง"
        // เพราะ NetworkList players จะหายไปหลังจากนี้
        if (players != null)
        {
            CachedNames.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                var n = p.playerName.ToString();
                if (!string.IsNullOrWhiteSpace(n))
                {
                    CachedNames[p.clientId] = n;
                }
            }
        }

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // ไม่ต้องเคลียร์ CachedNames เพราะจะใช้ต่อใน GameScene
        players = null;
        if (Instance == this) Instance = null;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // กันไม่ให้ใส่ซ้ำ
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
                return;
        }

        string defaultName = $"P{clientId}";

        var ps = new PlayerStateNet
        {
            clientId = clientId,
            playerName = (FixedString32Bytes)defaultName,
            characterIndex = -1,
            ready = false
        };
        players.Add(ps);

        // เซ็ต default ลง cache ไว้ก่อน (เผื่อกรณีชื่อจริงยังไม่มา)
        CachedNames[clientId] = defaultName;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].clientId == clientId)
            {
                players.RemoveAt(i);
            }
        }

        CachedNames.Remove(clientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetNameServerRpc(string name, ServerRpcParams rpc = default)
    {
        var cid = rpc.Receive.SenderClientId;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == cid)
            {
                var p = players[i];
                string finalName = string.IsNullOrWhiteSpace(name) ? $"P{cid}" : name.Trim();
                p.playerName = (FixedString32Bytes)finalName;
                players[i] = p;

                // อัปเดต cache ด้วย
                CachedNames[cid] = finalName;
                break;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SelectCharacterServerRpc(int charIndex, ServerRpcParams rpc = default)
    {
        var cid = rpc.Receive.SenderClientId;

        if (characterNames == null || charIndex < 0 || charIndex >= characterNames.Length) return;

        // ห้ามซ้ำ (ยกเว้นคนเดิมเลือกตัวเดิม)
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].characterIndex == charIndex && players[i].clientId != cid)
                return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == cid)
            {
                var p = players[i];
                p.characterIndex = charIndex;
                p.ready = false;
                players[i] = p;

                ApplyCharacterToHero(cid, charIndex);
                break;
            }
        }
    }

    private void ApplyCharacterToHero(ulong clientId, int charIndex)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            return;

        var playerObj = client.PlayerObject;
        if (!playerObj) return;

        var hero = playerObj.GetComponent<HeroControllerNet>();
        if (!hero) return;

        hero.ServerSetCharacterIndex(charIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready, ServerRpcParams rpc = default)
    {
        var cid = rpc.Receive.SenderClientId;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == cid)
            {
                var p = players[i];

                if (p.characterIndex >= 0)
                    p.ready = ready;

                players[i] = p;
                break;
            }
        }

        TryStartGame();
    }

    public bool AllReady()
    {
        if (players == null || players.Count == 0) return false;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].characterIndex < 0 || !players[i].ready)
                return false;
        }

        return true;
    }

    private void TryStartGame()
    {
        if (!IsServer) return;
        if (!AllReady()) return;

        Debug.Log("[Lobby] All players ready → Loading GameSceneNet");
        NetworkManager.SceneManager.LoadScene("GameSceneNet", LoadSceneMode.Single);
    }

    // helper ใช้จาก scene อื่น
    public static string GetCachedPlayerName(ulong clientId)
    {
        if (CachedNames.TryGetValue(clientId, out var n) &&
            !string.IsNullOrWhiteSpace(n))
        {
            return n;
        }
        return null;
    }
}
