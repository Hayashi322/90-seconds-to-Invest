// LobbyBootstrap.cs
using System;
using System.Threading.Tasks;
using UnityEngine;

public class LobbyBootstrap : MonoBehaviour
{
   /* private async void Start()
    {
        string role = PlayerPrefs.GetString(MainMenu.PendingRoleKey, "");
        string joinCode = PlayerPrefs.GetString(MainMenu.PendingJoinKey, "");

        PlayerPrefs.DeleteKey(MainMenu.PendingRoleKey);
        PlayerPrefs.DeleteKey(MainMenu.PendingJoinKey);
        PlayerPrefs.Save();

        try
        {
            if (role == "host")
            {
                await HostSingleton.Instance.GameManager.StartHostAsync(); 
            }
            else if (role == "client")
            {
                await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LobbyBootstrap] Failed: {e.Message}");
        }
    }*/
}
