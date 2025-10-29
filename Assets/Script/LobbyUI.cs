using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Refs")]
    public LobbyManager lobby;

    [Header("Left Slots (3 ช่อง)")]
    public Image[] slotPortraits;              // 3 รูป
    public TextMeshProUGUI[] slotNameTexts;    // 3 ชื่อ
    public TextMeshProUGUI[] slotStateTexts;   // 3 สถานะ

    [Header("Character Portraits (index ตรงกับ LobbyManager.characterNames)")]
    public Sprite[] characterSprites;

    [Header("Right Side")]
    public Button[] characterButtons;          // ปุ่มรูปตัวละคร
    public Button readyButton;
    public TextMeshProUGUI readyButtonText;    // ข้อความบนปุ่ม

    [Header("Hint")]
    public TextMeshProUGUI lobbyHintText;

    private void Start()
    {
        if (!lobby) lobby = LobbyManager.Instance;

        // map ปุ่มเลือกตัวละคร
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int idx = i;
            characterButtons[i].onClick.AddListener(() => OnPickCharacter(idx));
        }

        if (readyButton) readyButton.onClick.AddListener(OnToggleReady);

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
        if (me.HasValue) lobby.SetReadyServerRpc(!me.Value.ready);
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

        // ช่องซ้าย
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
                    slotStateTexts[i].color = p.ready ? new Color(0.2f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.25f);
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
                    : "เลือกตัวละครก่อน";
        }

        // ปุ่มตัวละคร: disable ถ้าถูกคนอื่นจอง (ยกเว้นเป็นตัวที่เราเลือกอยู่)
        for (int i = 0; i < characterButtons.Length; i++)
        {
            bool taken = false;
            for (int j = 0; j < lobby.players.Count; j++)
                if (lobby.players[j].characterIndex == i && lobby.players[j].clientId != NetworkManager.Singleton.LocalClientId)
                {
                    taken = true;
                    break;
                }

            characterButtons[i].interactable = !taken || (me.HasValue && me.Value.characterIndex == i);
        }

        // ข้อความแนะนำ
        if (lobbyHintText)
            lobbyHintText.text = lobby.AllReady() ? "ทุกคนพร้อมแล้ว • โฮสต์กดเริ่มได้" : "เลือกตัวละครไม่ซ้ำ แล้วกด ‘พร้อม’";
    }
}
