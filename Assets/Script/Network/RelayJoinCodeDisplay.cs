using TMPro;
using UnityEngine;

public class RelayJoinCodeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;     // ลาก TMP_Text มาวาง
    [SerializeField] private GameObject panel;          // กล่องที่โชว์โค้ด (optional)

    void OnEnable()
    {
        var host = HostSingleton.Instance;
        if (host?.GameManager == null) return;

        host.GameManager.JoinCodeChanged += OnJoinCode;
        // เผื่อ Host สร้างเสร็จก่อนเปิด UI:
        var current = host.GameManager.JoinCode;
        if (!string.IsNullOrEmpty(current)) OnJoinCode(current);
    }

    void OnDisable()
    {
        var host = HostSingleton.Instance;
        if (host?.GameManager == null) return;
        host.GameManager.JoinCodeChanged -= OnJoinCode;
    }

    private void OnJoinCode(string code)
    {
        if (joinCodeText) joinCodeText.text = code;
        if (panel) panel.SetActive(true);
    }

    // ปุ่ม 'Copy'
    public void CopyJoinCode()
    {
        if (!joinCodeText) return;
        GUIUtility.systemCopyBuffer = joinCodeText.text;
        Debug.Log("[Relay] Join code copied.");
    }
}
