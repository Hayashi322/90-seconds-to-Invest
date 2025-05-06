using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Image characterPreviewImage;
    public Button[] characterButtons;
    public Sprite[] characterSprites;

    private int selectedIndex = -1;

    void Start()
    {
        playerNameText.text = PlayerData.Instance.playerName;
        characterPreviewImage.sprite = null; // ไม่มีรูปเริ่มต้น
        characterPreviewImage.color = new Color(1, 1, 1, 0); // โปร่งใส
    }


    public void SelectCharacter(int index)
    {
        selectedIndex = index;

        for (int i = 0; i < characterButtons.Length; i++)
        {
            characterButtons[i].interactable = i != index;
        }

        characterPreviewImage.sprite = characterSprites[index];
        characterPreviewImage.color = Color.white; // ทำให้กลับมาเห็นชัด
        PlayerData.Instance.selectedCharacterIndex = index;
    }


    public void StartGame()
    {
        if (selectedIndex == -1)
        {
            Debug.Log("กรุณาเลือกตัวละครก่อนเริ่มเกม");
            return;
        }

        SceneManager.LoadScene("GameScene");
    }

    public void ExitLobby()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
