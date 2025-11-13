using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    [Header("Character Catalog (index ต้องตรงกับรูปใน UI)")]
    public string[] characterNames; // เช่น ["Hero A","Hero B","Hero C","Hero D"]

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

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // เมื่อ LobbyManager spawn ขึ้นมา ให้เพิ่ม client ที่เชื่อมอยู่แล้วเข้าลิสต์ทันที
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                OnClientConnected(clientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

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

        var ps = new PlayerStateNet
        {
            clientId = clientId,
            playerName = (FixedString32Bytes)$"P{clientId}",
            characterIndex = -1,
            ready = false
        };
        players.Add(ps);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].clientId == clientId)
            {
                // ลบออกจากลิสต์ → ตัวละครที่เขาเลือกจะถูกคืนให้ว่างอัตโนมัติ
                players.RemoveAt(i);
            }
        }
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
                p.playerName = (FixedString32Bytes)(string.IsNullOrWhiteSpace(name) ? $"P{cid}" : name.Trim());
                players[i] = p;
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
                p.ready = false; // เปลี่ยนตัวละครแล้วให้ไม่พร้อม
                players[i] = p;

                // ส่งตัวละครไปให้ HeroControllerNet ของ player นี้ใช้ในเกมจริง
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

                // ต้องเลือกตัวละครก่อนถึงจะ Ready ได้
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
}
