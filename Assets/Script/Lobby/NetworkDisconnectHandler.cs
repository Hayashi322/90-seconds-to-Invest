using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        ulong localId = nm.LocalClientId;

        // ===== กรณี 1: Host หลุด → client ทุกคนกลับเมนู =====
        // ใน NGO host (server) มี clientId = 0 เสมอ
        if (!nm.IsServer && clientId == 0)
        {
            Debug.Log("[Net] Host disconnected. Returning to menu...");
            NetworkReturnToMenu.ReturnToMenu();
            return;
        }

        // ===== กรณี 2: เราเองโดนตัด (จะเป็น host หรือ client ก็ได้) =====
        if (clientId == localId)
        {
            Debug.Log("[Net] Local player disconnected. Returning to menu...");
            NetworkReturnToMenu.ReturnToMenu();
        }
    }
}


