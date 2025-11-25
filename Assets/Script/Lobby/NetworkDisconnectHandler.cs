using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkDisconnectHandler : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";


    /*private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }*/

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

        // ========= กรณี 1: Host หลุด → Client ทุกคนออก =========
        // Host มี clientId = 0 เสมอ
        if (!nm.IsServer && clientId == 0)
        {
            Debug.Log("[Net] Host disconnected. Returning to menu...");
            SceneManager.LoadScene(menuSceneName);
            return;
        }

        // ========= กรณี 2: เราเองหลุด =========
        if (clientId == localId)
        {
            Debug.Log("[Net] Local player disconnected. Returning to menu...");
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
