using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Name / Profile")]
    [SerializeField] private NameSaveUI nameSaveUI;   // <<--- ลากมาจาก UI ตั้งชื่อ

    [Header("UI Refs")]
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        if (quitButton)
            quitButton.onClick.AddListener(QuitGame);
    }

    public async void StartHost()
    {
        // เช็คว่าตั้งชื่อแล้วหรือยัง
        if (nameSaveUI && !nameSaveUI.EnsureNameSavedOrWarn())
        {
            // ยังไม่ตั้งชื่อ → กระพริบเตือนแล้วไม่ไปต่อ
            return;
        }

        HostSingleton.Instance.CreateHost();
        await HostSingleton.Instance.GameManager.StartHostAsync();
        // RelayJoinCodeDisplay จะอัปเดตเองถ้ามีในฉาก
    }

    public async void StartClient()
    {
        //  เช็คว่าตั้งชื่อแล้วหรือยัง
        if (nameSaveUI && !nameSaveUI.EnsureNameSavedOrWarn())
        {
            // ยังไม่ตั้งชื่อ → กระพริบเตือนแล้วไม่ไปต่อ
            return;
        }

        string code = joinCodeField ? joinCodeField.text : string.Empty;
        await ClientSingleton.Instance.GameManager.StartClientAsync(code);
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
