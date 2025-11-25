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
    private bool isBound = false;                  // กันสมัคร event ซ้ำ

    private void OnEnable()
    {
        // กันไม่ให้เหลือ coroutine เก่าค้าง
        StopAllCoroutines();
        isBound = false;

        StartCoroutine(BindRoutine());
    }

    private void OnDisable()
    {
        // ยกเลิก event ถ้าเคย bind แล้ว
        if (sync != null && isBound)
        {
            sync.JoinCode.OnValueChanged -= OnJoinCodeChanged;
        }

        sync = null;
        isBound = false;
    }

    private IEnumerator BindRoutine()
    {
        // รอให้มี NetworkManager และเริ่มฟัง (Host / Client ต่อแล้ว)
        while (isActiveAndEnabled &&
               (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening))
        {
            yield return null;
        }
        if (!isActiveAndEnabled) yield break;

        // รอจนกว่าจะมี RelayJoinCodeSync ที่ spawn แล้วในซีนนี้
        while (isActiveAndEnabled &&
               (RelayJoinCodeSync.Instance == null || !RelayJoinCodeSync.Instance.IsSpawned))
        {
            yield return null;
        }
        if (!isActiveAndEnabled) yield break;

        sync = RelayJoinCodeSync.Instance;

        // ถ้าเคย bind แล้วไม่ต้องทำอีก
        if (sync != null && !isBound)
        {
            sync.JoinCode.OnValueChanged += OnJoinCodeChanged;
            isBound = true;

            // เซ็ตค่าเริ่มต้น (กรณี Host เซ็ต JoinCode มาก่อนแล้ว)
            OnJoinCodeChanged(default, sync.JoinCode.Value);
        }
    }

    private void OnJoinCodeChanged(FixedString32Bytes oldVal, FixedString32Bytes newVal)
    {
        string code = newVal.ToString();
        Debug.Log($"[RelayUI] JoinCode changed to: {code}");

        if (joinCodeText)
            joinCodeText.text = code;

        if (panel)
            panel.SetActive(!string.IsNullOrEmpty(code));
    }

    // ปุ่ม Copy
    public void CopyJoinCode()
    {
        if (!joinCodeText) return;

        GUIUtility.systemCopyBuffer = joinCodeText.text;
        Debug.Log("[Relay] Join code copied.");
    }
}
