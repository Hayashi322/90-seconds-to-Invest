using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;

    public string playerName;
    public int selectedCharacterIndex;

    private const string PlayerNameKey = "player_name";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // โหลดชื่อจาก PlayerPrefs ตอนเริ่มเกม
            playerName = PlayerPrefs.GetString(PlayerNameKey, "");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerName(string newName)
    {
        newName = newName?.Trim() ?? "";
        playerName = newName;

        PlayerPrefs.SetString(PlayerNameKey, newName);
        PlayerPrefs.Save();
    }
}
