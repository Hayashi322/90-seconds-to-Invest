using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class OpenCanvas : MonoBehaviour
{
    [Header("Canvas Groups (auto-fill if empty)")]
    [SerializeField] private CanvasGroup[] canvases;

    [Header("Overlay / Input Blocker (optional)")]
    [SerializeField] private GameObject blockRaycast;

    [Header("Owner hero (auto-bind if missing)")]
    [SerializeField] private HeroControllerNet controllerNet;

    [Header("Behavior")]
    [SerializeField] private bool closeWithEsc = true;
    [SerializeField] private bool pauseWithTimescale = false;   // หยุดเกมด้วย Time.timeScale
    [SerializeField, Range(0f, 1f)] private float fadeAlpha = 1f;

    [Header("Events")]
    public UnityEvent<int> OnOpenIndex;   // ส่ง index แคนวาสที่เปิด
    public UnityEvent OnCloseAll;

    private int _current = -1;
    private bool _initialized;

    #region Unity lifecycle
    private void Awake()
    {
        TryAutoFillCanvases();
        TryAutoBindOwner();
        InitAllClosed();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            TryAutoFillCanvases();
        }
    }
#endif

    private void Update()
    {
        if (!closeWithEsc) return;

        // ป้องกันกรณีโฟกัสอยู่บน input field แล้วกด ESC ให้ปิด
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllUI();
        }
    }
    #endregion

    // ========= Public API =========

    /// <summary>เปิดตามลำดับ index ในอาเรย์ canvases</summary>
    public void openCanvas(int number)
    {
        if (!EnsureReady()) return;
        if (number < 0 || number >= canvases.Length)
        {
            Debug.LogWarning($"[OpenCanvas] index OOR: {number}");
            return;
        }

        if (_current == number) return; // กันเปิดซ้ำ

        ShowOnly(number);

        // แจ้งระบบเกมให้หยุด input การเดิน
        controllerNet?.SetUIOpen(true);

        // บังคลิกทั้งฉาก
        if (blockRaycast) blockRaycast.SetActive(true);

        if (pauseWithTimescale) Time.timeScale = 0f;

        OnOpenIndex?.Invoke(number);
    }

    /// <summary>เปิดด้วยชื่อ GameObject ของ CanvasGroup (เช่น "bankPanel")</summary>
    public void OpenByName(string canvasObjectName)
    {
        int idx = FindCanvasIndexByName(canvasObjectName);
        if (idx >= 0) openCanvas(idx);
        else Debug.LogWarning($"[OpenCanvas] canvas name not found: {canvasObjectName}");
    }

    /// <summary>เปิดโดยแม็ปจาก waypointId → index (ส่ง delegate แม็ปเข้ามา)</summary>
    public void OpenByWaypointId(int waypointId, Func<int, int> mapIdToIndex)
    {
        if (mapIdToIndex == null) { Debug.LogWarning("[OpenCanvas] mapIdToIndex null"); return; }
        int idx = mapIdToIndex.Invoke(waypointId);
        if (idx >= 0) openCanvas(idx);
    }

    /// <summary>ปิดทุกแคนวาส (สำหรับปุ่ม Close/ESC)</summary>
    public void closeCanvas() => CloseAllUI();

    // ========= Private helpers =========

    private bool EnsureReady()
    {
        if (!_initialized)
        {
            TryAutoFillCanvases();
            TryAutoBindOwner();
            InitAllClosed();
        }
        return canvases != null && canvases.Length > 0;
    }

    private void TryAutoFillCanvases()
    {
        if (canvases == null || canvases.Length == 0)
        {
            canvases = GetComponentsInChildren<CanvasGroup>(true);
        }
    }

    private void TryAutoBindOwner()
    {
        if (controllerNet) return;

        // 1) ลองจาก LocalPlayerObject (Netcode)
        var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj) controllerNet = localObj.GetComponent<HeroControllerNet>();

        // 2) fallback: หาในซีน (รวม inactive)
#if UNITY_2023_1_OR_NEWER
        if (!controllerNet) controllerNet = FindFirstObjectByType<HeroControllerNet>(FindObjectsInactive.Include);
#else
        if (!controllerNet) controllerNet = FindObjectOfType<HeroControllerNet>(true);
#endif
    }

    private void InitAllClosed()
    {
        CloseAllImmediate();
        if (blockRaycast) blockRaycast.SetActive(false);
        _current = -1;
        _initialized = true;
    }

    private void ShowOnly(int index)
    {
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (!c) continue;

            bool active = (i == index);
            c.alpha = active ? fadeAlpha : 0f;
            c.blocksRaycasts = active;
            c.interactable = active;

            // ถ้าแคนวาสเป็น GameObject ใหญ่ที่มีอนิเม/เสียง อาจเปิด/ปิดตัวมันด้วย
            if (c.gameObject.activeSelf != active) c.gameObject.SetActive(active);
        }
        _current = index;
    }

    private void CloseAllUI()
    {
        CloseAllImmediate();

        if (blockRaycast) blockRaycast.SetActive(false);
        controllerNet?.SetUIOpen(false);

        if (pauseWithTimescale) Time.timeScale = 1f;

        OnCloseAll?.Invoke();
        _current = -1;
    }

    private void CloseAllImmediate()
    {
        if (canvases == null) return;
        foreach (var c in canvases)
        {
            if (!c) continue;
            c.alpha = 0f;
            c.blocksRaycasts = false;
            c.interactable = false;
            if (c.gameObject.activeSelf) c.gameObject.SetActive(false);
        }
    }

    private int FindCanvasIndexByName(string name)
    {
        if (string.IsNullOrEmpty(name) || canvases == null) return -1;
        for (int i = 0; i < canvases.Length; i++)
        {
            var c = canvases[i];
            if (!c) continue;
            if (string.Equals(c.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
