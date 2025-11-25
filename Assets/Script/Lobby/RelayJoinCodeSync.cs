using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RelayJoinCodeSync : NetworkBehaviour
{
    public static RelayJoinCodeSync Instance { get; private set; }

    // NetworkVariable เก็บโค้ดห้อง ให้ทุก client อ่านได้
    public NetworkVariable<FixedString32Bytes> JoinCode =
        new NetworkVariable<FixedString32Bytes>(
            "",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // ✅ ฝั่ง Server เซ็ตค่าแรกจาก cache (ที่ HostGameManager เก็บไว้)
        if (IsServer)
        {
            if (!string.IsNullOrEmpty(RelayJoinCodeCache.LastJoinCode))
            {
                JoinCode.Value = RelayJoinCodeCache.LastJoinCode;
                Debug.Log($"[RelaySync] Set JoinCode = {JoinCode.Value}");
            }
            else
            {
                Debug.LogWarning("[RelaySync] RelayJoinCodeCache.LastJoinCode is empty");
            }
        }
    }
}
