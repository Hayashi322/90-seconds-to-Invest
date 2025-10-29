﻿using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

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
        }

        // client ส่งชื่อเริ่มต้นให้ server
        if (IsClient && IsOwner)
        {
            var name = PlayerPrefs.GetString("player_name", $"P{OwnerClientId}");
            SetNameServerRpc(name);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

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
            if (players[i].clientId == clientId)
                players.RemoveAt(i);
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
                p.characterIndex = p.characterIndex; // no-op แค่ชัดเจน
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

        // ห้ามซ้ำ
        for (int i = 0; i < players.Count; i++)
            if (players[i].characterIndex == charIndex && players[i].clientId != cid)
                return;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == cid)
            {
                var p = players[i];
                p.characterIndex = charIndex;
                p.ready = false; // เปลี่ยนตัวละครแล้วไม่พร้อม
                players[i] = p;
                break;
            }
        }
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
                if (p.characterIndex >= 0) p.ready = ready; // ต้องเลือกตัวละครก่อน
                players[i] = p;
                break;
            }
        }
    }

    public bool AllReady()
    {
        if (players == null || players.Count == 0) return false;

        for (int i = 0; i < players.Count; i++)
            if (players[i].characterIndex < 0 || !players[i].ready)
                return false;

        return true;
    }
}
