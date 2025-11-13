using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [Header("Refs")]
    public LobbyManager lobby;

    [Header("Left Slots (4 ช่อง)")]
    public Image[] slotPortraits;              // รูปตัวละคร
    public TextMeshProUGUI[] slotNameTexts;    // ชื่อผู้เล่น
    public TextMeshProUGUI[] slotStateTexts;   // สถานะพร้อม

    [Header("Character Portraits (index ตรงกับ LobbyManager.characterNames)")]
    public Sprite[] characterSprites;

    [Header("Right Side")]
    public Button[] characterButtons;          // ปุ่มเลือกรูปตัวละคร
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;    // ข้อความบนปุ่ม Ready

    [Header("Bottom Buttons")]
    public Button exitLobbyButton;             // ปุ่มออกจากลอบบี้

    [Header("Hint")]
    public TextMeshProUGUI lobbyHintText;      // ข้อความแนะนำด้านบน

    private void Start()
    {
        if (!lobby) lobby = LobbyManager.Instance;

        // ✅ ส่งชื่อของเราขึ้นไปให้ server อัปเดตใน players list
        SendMyNameToServer();

        // map ปุ่มเลือกตัวละคร
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int idx = i;
            if (characterButtons[i])
                characterButtons[i].onClick.AddListener(() => OnPickCharacter(idx));
        }

        if (readyButton) readyButton.onClick.AddListener(OnToggleReady);
        if (exitLobbyButton) exitLobbyButton.onClick.AddListener(OnExitLobby);

        InvokeRepeating(nameof(RefreshUI), 0.1f, 0.2f);
    }

    private void OnPickCharacter(int index)
    {
        if (!lobby) return;
        lobby.SelectCharacterServerRpc(index);
    }

    private void OnToggleReady()
    {
        if (!lobby) return;
        var me = FindMe();
        if (me.HasValue)
            lobby.SetReadyServerRpc(!me.Value.ready);
    }

    private void OnExitLobby()
    {
        // ปิดการเชื่อมต่อ Network (ทั้ง Host และ Client ใช้ได้เหมือนกัน)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // กลับไปหน้า MainMenu
        SceneManager.LoadScene("MainMenu");
    }

    private LobbyManager.PlayerStateNet? FindMe()
    {
        if (!lobby || lobby.players == null) return null;

        ulong myId = NetworkManager.Singleton.LocalClientId;
        for (int i = 0; i < lobby.players.Count; i++)
            if (lobby.players[i].clientId == myId)
                return lobby.players[i];

        return null;
    }

    private void RefreshUI()
    {
        if (!lobby || lobby.players == null) return;

        // ช่องซ้าย (Player Slots)
        for (int i = 0; i < slotPortraits.Length; i++)
        {
            if (i < lobby.players.Count)
            {
                var p = lobby.players[i];

                // รูป
                if (slotPortraits[i])
                {
                    if (p.characterIndex >= 0 && p.characterIndex < characterSprites.Length)
                    {
                        slotPortraits[i].sprite = characterSprites[p.characterIndex];
                        slotPortraits[i].color = Color.white;
                    }
                    else
                    {
                        slotPortraits[i].sprite = null;
                        slotPortraits[i].color = new Color(1, 1, 1, 0.2f);
                    }
                }

                // ชื่อ
                if (slotNameTexts[i]) slotNameTexts[i].text = p.playerName.ToString();

                // สถานะ
                if (slotStateTexts[i])
                {
                    slotStateTexts[i].text = p.ready ? "พร้อม" : "ยังไม่พร้อม";
                    
                }
            }
            else
            {
                if (slotPortraits[i])
                {
                    slotPortraits[i].sprite = null;
                    slotPortraits[i].color = new Color(1, 1, 1, 0.1f);
                }
                if (slotNameTexts[i]) slotNameTexts[i].text = "-";
                if (slotStateTexts[i]) slotStateTexts[i].text = "";
            }
        }

        // ปุ่มพร้อม
        var me = FindMe();
        if (readyButtonText)
        {
            readyButtonText.text =
                (me.HasValue && me.Value.characterIndex >= 0)
                    ? (me.Value.ready ? "ยกเลิกพร้อม" : "พร้อม")
                    : "พร้อม";
        }

        if (readyButton)
        {
            bool canReady = me.HasValue && me.Value.characterIndex >= 0;
            readyButton.interactable = canReady;
        }

        // ปุ่มตัวละคร: disable ถ้าถูกคนอื่นจอง (ยกเว้นเป็นตัวที่เราเลือกอยู่เอง)
        for (int i = 0; i < characterButtons.Length; i++)
        {
            bool taken = false;
            for (int j = 0; j < lobby.players.Count; j++)
            {
                if (lobby.players[j].characterIndex == i &&
                    lobby.players[j].clientId != NetworkManager.Singleton.LocalClientId)
                {
                    taken = true;
                    break;
                }
            }

            if (characterButtons[i])
                characterButtons[i].interactable = !taken || (me.HasValue && me.Value.characterIndex == i);
        }

        // ข้อความแนะนำ
        if (lobbyHintText)
        {
            lobbyHintText.text = lobby.AllReady()
                ? "ทุกคนพร้อมแล้ว • ระบบจะเริ่มเกมอัตโนมัติ"
                : "เลือกตัวละคร";
        }
    }
    private void SendMyNameToServer()
    {
        if (!lobby) return;

        string localName = PlayerPrefs.GetString(
            "player_name",
            $"P{NetworkManager.Singleton.LocalClientId}"
        );

        lobby.SetNameServerRpc(localName);
    }
}
