using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class NetworkReturnToMenu
{
    private const string MenuSceneName = "MainMenu";

    public static void ReturnToMenu()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null)
        {
            if (nm.IsListening)
            {
                UnityEngine.Debug.Log("[ReturnToMenu] Shutdown Netcode and leave room.");
                nm.Shutdown();           // แค่ shutdown พอ
            }
        }

        // เคลียร์ static ต่าง ๆ ที่ใช้ข้ามเกม
        LobbyManager.CachedNames.Clear();
        GameResultManager.ResetStatics();
        RelayJoinCodeCache.LastJoinCode = string.Empty;

        // กลับหน้าเมนู
        SceneManager.LoadScene(MenuSceneName);
    }
}
