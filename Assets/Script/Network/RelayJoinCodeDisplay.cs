using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RelayJoinCodeDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText; // ตัวหนังสือแสดงโค้ด
    [SerializeField] private GameObject panel;      // กล่อง/Panel ที่โชว์โค้ด

    private RelayJoinCodeSync sync;

    private void OnEnable()
    {
        StartCoroutine(BindRoutine());
    }

    private void OnDisable()
    {
        if (sync != null)
        {
            sync.JoinCode.OnValueChanged -= OnJoinCodeChanged;
        }
    }

    private IEnumerator BindRoutine()
    {
        // ✅ รอ Netcode เริ่ม
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        // ✅ รอจนกว่าจะมี RelayJoinCodeSync ที่ Spawn แล้ว
        while (RelayJoinCodeSync.Instance == null || !RelayJoinCodeSync.Instance.IsSpawned)
            yield return null;

        sync = RelayJoinCodeSync.Instance;

        // สมัคร event เวลา JoinCode เปลี่ยน
        sync.JoinCode.OnValueChanged += OnJoinCodeChanged;

        // เซ็ตค่าเริ่มต้น (เผื่อ Host เซ็ตไว้แล้ว)
        OnJoinCodeChanged(default, sync.JoinCode.Value);
    }

    private void OnJoinCodeChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        string code = newVal.ToString();
        Debug.Log($"[RelayUI] JoinCode changed to: {code}");

        if (joinCodeText) joinCodeText.text = code;
        if (panel) panel.SetActive(!string.IsNullOrEmpty(code));
    }

    // ปุ่ม Copy ยังใช้ได้เหมือนเดิม
    public void CopyJoinCode()
    {
        if (!joinCodeText) return;
        GUIUtility.systemCopyBuffer = joinCodeText.text;
        Debug.Log("[Relay] Join code copied.");
    }
}
