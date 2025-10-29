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
    private const string GameSceneName = "GameSceneNet";

    public string JoinCode { get; private set; } = string.Empty;
    public event Action<string> JoinCodeChanged;

    public async Task StartHostAsync()
    {
        // (มี Initialize + Auth ตามที่แก้ไปก่อนหน้าแล้ว)
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[Relay] JoinCode = {JoinCode}");
            JoinCodeChanged?.Invoke(JoinCode); // <-- แจ้ง UI
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
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }
}
