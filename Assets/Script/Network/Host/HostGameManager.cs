using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager
{
    private Allocation allocation;
    private const int MaxConnections = 4;

    private const string LobbySceneName = "LobbyScene";

    public string JoinCode { get; private set; } = string.Empty;
    public event Action<string> JoinCodeChanged;

    public async Task StartHostAsync()
    {
        // ---------- 1) เช็ค NetworkManager ----------
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[HostGameMgr] NetworkManager.Singleton == null");
            return;
        }

        // ---------- 2) ถ้ามี session เดิมรันค้างอยู่ ให้ Shutdown ก่อน ----------
        if (nm.IsListening)
        {
            Debug.LogWarning("[HostGameMgr] Network already running. Shutdown old session first.");
            nm.Shutdown();
            // รอเฟรมหนึ่งให้ Netcode เคลียร์ตัวเอง
            await Task.Yield();
        }

        // ---------- 3) ขอ Allocation + JoinCode ใหม่จาก Relay ----------
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] JoinCode = {JoinCode}");

            // ถ้ามีตัว sync join code ไว้ ก็อัปเดตที่นี่ได้
            RelayJoinCodeCache.LastJoinCode = JoinCode;
            JoinCodeChanged?.Invoke(JoinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"[HostGameMgr] Relay CreateAllocation failed: {e}");
            return;
        }

        // ---------- 4) เซ็ต UnityTransport ให้ใช้ Relay ----------
        var transport = nm.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[HostGameMgr] UnityTransport not found on NetworkManager.");
            return;
        }

        var relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        // ---------- 5) StartHost ----------
        if (!nm.StartHost())
        {
            Debug.LogError("[HostGameMgr] StartHost() failed.");
            return;
        }

        Debug.Log("[HostGameMgr] Host started.");

        // ---------- 6) กัน bug host ไม่มี PlayerObject (รอบที่ 2 เป็นต้นไป) ----------
        EnsureHostPlayerObject(nm);

        // ---------- 7) เข้า LobbyScene เป็น Network Scene ----------
        nm.SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// ให้แน่ใจว่า Host มี PlayerObject เสมอ
    /// (บางครั้งรอบที่ 2 Netcode จะไม่ spawn ให้เอง)
    /// </summary>
    private void EnsureHostPlayerObject(NetworkManager nm)
    {
        if (nm == null) return;

        var localClient = nm.LocalClient;
        if (localClient == null)
        {
            Debug.LogWarning("[HostGameMgr] LocalClient is null right after StartHost.");
            return;
        }

        // ถ้ามี PlayerObject อยู่แล้วก็จบ
        if (localClient.PlayerObject != null)
        {
            Debug.Log("[HostGameMgr] Host already has PlayerObject.");
            return;
        }

        // ดึง prefab จาก NetworkConfig
        var playerPrefab = nm.NetworkConfig.PlayerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError("[HostGameMgr] NetworkConfig.PlayerPrefab is null (ตั้งค่าใน NetworkManager ยัง?).");
            return;
        }

        // สร้าง object แล้ว Spawn เป็น PlayerObject ของ host
        var obj = UnityEngine.Object.Instantiate(
            playerPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[HostGameMgr] PlayerPrefab ไม่มี NetworkObject!");
            UnityEngine.Object.Destroy(obj);
            return;
        }

        netObj.SpawnAsPlayerObject(nm.LocalClientId, true);
        Debug.Log("[HostGameMgr] Manually spawned PlayerObject for host.");
    }
}
