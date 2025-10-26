using System;
using UnityEngine;
using UnityEngine.Events;
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
    [SerializeField] private bool pauseWithTimescale = false;
    [SerializeField, Range(0f, 1f)] private float fadeAlpha = 1f;
    [SerializeField] private bool bringToFront = true;        // ดันขึ้นหน้าสุด
    [SerializeField] private int sortingOrderOnOpen = 1000;   // ถ้า bringToFront = true

    [Header("Events")]
    public UnityEvent<int> OnOpenIndex;
    public UnityEvent OnCloseAll;

    private int _current = -1;
    private bool _initialized;

    // ---------- Unity lifecycle ----------
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
            TryAutoFillCanvases();
    }
#endif

    private void Update()
    {
        if (!closeWithEsc) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            CloseAllUI();
    }

    // ---------- Public API ----------
    /// <summary>เปิดแผงตาม index ในอาเรย์ canvases</summary>
    public void openCanvas(int number)
    {
        if (!EnsureReady()) return;

        if (number < 0 || number >= canvases.Length)
        {
            Debug.LogWarning($"[OpenCanvas] index out of range: {number} (0..{canvases.Length - 1})");
            return;
        }

        if (_current == number)
        {
            // เปิดซ้ำก็ไม่ต้องทำอะไร
            Debug.Log($"[OpenCanvas] already open index={number} ({canvases[number]?.name})");
            return;
        }

        ShowOnly(number);

        controllerNet?.SetUIOpen(true);             // ล็อกการเดิน/อินพุตโลก
        if (blockRaycast) blockRaycast.SetActive(true);
        if (pauseWithTimescale) Time.timeScale = 0f;

        OnOpenIndex?.Invoke(number);
        Debug.Log($"[OpenCanvas] OPEN index={number} name={canvases[number]?.name}");
    }

    /// <summary>ปิดทุกแผง</summary>
    public void closeCanvas() => CloseAllUI();

    /// <summary>เปิดตามชื่อ GameObject ของแผง (เช่น “BankPanel”)</summary>
    public void OpenByName(string canvasObjectName)
    {
        int idx = FindCanvasIndexByName(canvasObjectName);
        if (idx >= 0) openCanvas(idx);
        else Debug.LogWarning($"[OpenCanvas] canvas name not found: {canvasObjectName}");
    }

    /// <summary>เปิดโดยแมป waypointId -> index</summary>
    public void OpenByWaypointId(int waypointId, Func<int, int> mapIdToIndex)
    {
        if (mapIdToIndex == null) { Debug.LogWarning("[OpenCanvas] mapIdToIndex is null"); return; }
        int idx = mapIdToIndex.Invoke(waypointId);
        if (idx >= 0) openCanvas(idx);
        else Debug.LogWarning($"[OpenCanvas] map returned invalid index for waypoint {waypointId}");
    }

    // ---------- Internal ----------
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
            canvases = GetComponentsInChildren<CanvasGroup>(true);
    }

    private void TryAutoBindOwner()
    {
        if (controllerNet) return;

        var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj) controllerNet = localObj.GetComponent<HeroControllerNet>();

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
        // ปิดตัวอื่น
        for (int i = 0; i < canvases.Length; i++)
        {
            var cg = canvases[i];
            if (!cg) continue;
            bool active = (i == index);

            if (!active)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
                if (cg.gameObject.activeSelf) cg.gameObject.SetActive(false);
            }
        }

        // เป้า
        var target = canvases[index];
        if (!target) { _current = -1; return; }

        // 1) เปิดพาเรนต์ทุกชั้น + เคลียร์ CanvasGroup บนพาเรนต์
        ForceEnableHierarchy(target.gameObject);

        // 2) บังคับเปิด/คลิกได้
        target.gameObject.SetActive(true);
        target.alpha = fadeAlpha;
        target.blocksRaycasts = true;
        target.interactable = true;

        // 3) ดันขึ้นหน้าสุด (ลูปทุก Canvas ในสายพาเรนต์)
        var canvList = target.GetComponentsInParent<Canvas>(true);
        foreach (var c in canvList)
        {
            c.enabled = true;
            c.overrideSorting = true;
            if (c.sortingOrder < 1000) c.sortingOrder = 1000; // กันโดนบัง
        }

        // 4) เผื่อโดนเลื่อนตำแหน่ง/ขนาดจนหลุดจอ → รีเซ็ตให้อยู่กลาง
        var rt = target.GetComponent<RectTransform>();
        if (rt)
        {
            rt.SetAsLastSibling(); // ดัน sibling ขึ้นบนใน Canvas เดียวกัน
            if (Mathf.Abs(rt.anchoredPosition.x) > 10000 || Mathf.Abs(rt.anchoredPosition.y) > 10000)
                rt.anchoredPosition = Vector2.zero; // กันหลุดจอ by mistake
        }

        _current = index;
        Debug.Log($"[OpenCanvas] FORCED OPEN index={index} path={GetPath(target.transform)}", target);
    }

    private void ForceEnableHierarchy(GameObject go)
    {
        // เปิดพาเรนต์ทุกชั้น
        var t = go.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            // ถ้ามี CanvasGroup บนพาเรนต์ → ล้างทับ
            var cgs = t.GetComponents<CanvasGroup>();
            foreach (var cg in cgs)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
            t = t.parent;
        }
    }

    // แค่ไว้ดีบักดูเส้นทางใน Console
    private string GetPath(Transform tr)
    {
        System.Text.StringBuilder sb = new();
        while (tr != null) { sb.Insert(0, "/" + tr.name); tr = tr.parent; }
        return sb.ToString();
    }


    private void CloseAllUI()
    {
        CloseAllImmediate();

        if (blockRaycast) blockRaycast.SetActive(false);
        controllerNet?.SetUIOpen(false);
        if (pauseWithTimescale) Time.timeScale = 1f;

        OnCloseAll?.Invoke();
        _current = -1;

        Debug.Log("[OpenCanvas] CLOSE ALL");
    }

    private void CloseAllImmediate()
    {
        if (canvases == null) return;

        foreach (var cg in canvases)
        {
            if (!cg) continue;

            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // ไม่จำเป็นต้องปิด Canvas component แต่ปิด GameObject ให้ชัดไปเลย
            if (cg.gameObject.activeSelf)
                cg.gameObject.SetActive(false);
        }
    }

    private int FindCanvasIndexByName(string name)
    {
        if (string.IsNullOrEmpty(name) || canvases == null) return -1;
        for (int i = 0; i < canvases.Length; i++)
        {
            var cg = canvases[i];
            if (!cg) continue;
            if (string.Equals(cg.gameObject.name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
