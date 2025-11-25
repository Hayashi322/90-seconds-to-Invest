using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Refs")]
    public LobbyManager lobby;

    [Header("Left Slots (4 ช่อง)")]
    public Image[] slotPortraits;
    public TextMeshProUGUI[] slotNameTexts;
    public TextMeshProUGUI[] slotStateTexts;

    [Header("Character Portraits (index ต้องตรงกับ LobbyManager.characterNames)")]
    public Sprite[] characterSprites;

    [Header("Right Side")]
    public Button[] characterButtons;
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;

    [Header("Bottom Buttons")]
    public Button exitLobbyButton;

    [Header("Hint")]
    public TextMeshProUGUI lobbyHintText;

    private Sprite[] initialSlotSprites;

    private void Start()
    {
        if (!lobby) lobby = LobbyManager.Instance;

        initialSlotSprites = new Sprite[slotPortraits.Length];
        for (int i = 0; i < slotPortraits.Length; i++)
        {
            if (slotPortraits[i])
                initialSlotSprites[i] = slotPortraits[i].sprite;
        }

        SendMyNameToServer();

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
        Debug.Log("[Lobby] Player pressed Exit Lobby.");
        NetworkReturnToMenu.ReturnToMenu();
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

        // ===== ช่องซ้าย =====
        for (int i = 0; i < slotPortraits.Length; i++)
        {
            if (i < lobby.players.Count)
            {
                var p = lobby.players[i];

                if (slotPortraits[i])
                {
                    if (p.characterIndex >= 0 && p.characterIndex < characterSprites.Length)
                        slotPortraits[i].sprite = characterSprites[p.characterIndex];
                    else
                        slotPortraits[i].sprite = initialSlotSprites[i];

                    slotPortraits[i].color = Color.white;
                }

                if (slotNameTexts[i]) slotNameTexts[i].text = p.playerName.ToString();
                if (slotStateTexts[i]) slotStateTexts[i].text = p.ready ? "พร้อม" : "ยังไม่พร้อม";
            }
            else
            {
                if (slotPortraits[i])
                {
                    slotPortraits[i].sprite = initialSlotSprites[i];
                    slotPortraits[i].color = new Color(1f, 1f, 1f, 0.2f);
                }
                if (slotNameTexts[i]) slotNameTexts[i].text = "กำลังรอผู้เล่น";
                if (slotStateTexts[i]) slotStateTexts[i].text = "";
            }
        }

        // ===== ปุ่ม Ready =====
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

        // ===== ปุ่มตัวละคร =====
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
                characterButtons[i].interactable = !taken ||
                    (me.HasValue && me.Value.characterIndex == i);
        }

        // ===== ข้อความ Hint ด้านบน =====
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
