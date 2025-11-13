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

    // เดิมเป็น GameSceneNet
    // private const string GameSceneName = "GameSceneNet";
    private const string LobbySceneName = "LobbyScene";   // ✅ ชื่อซีนลอบบี้

    public string JoinCode { get; private set; } = string.Empty;
    public event Action<string> JoinCodeChanged;

    public async Task StartHostAsync()
    {
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] JoinCode = {JoinCode}");
            JoinCodeChanged?.Invoke(JoinCode);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartHost();

        // ✅ ให้เข้า LobbyScene เป็น Network Scene แทน GameSceneNet
        NetworkManager.Singleton.SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
    }
}

