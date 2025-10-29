using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        quitButton.onClick.AddListener(QuitGame);
    }

    public async void StartHost()
    {
        HostSingleton.Instance.CreateHost();
        // (ถ้ามี RelayJoinCodeDisplay อยู่ในฉาก มันจะ subscribe เองตอน OnEnable)
        await HostSingleton.Instance.GameManager.StartHostAsync();
        // หลังได้โค้ด อีเวนต์จะยิง → UI อัปเดตเอง
    }

    public async void StartClient()
    {
        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCodeField.text);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}
