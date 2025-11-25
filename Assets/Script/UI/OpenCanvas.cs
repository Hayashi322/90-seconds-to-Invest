using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

public class OpenCanvas : MonoBehaviour
{
    public static OpenCanvas Instance { get; private set; }

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
    [SerializeField] private bool bringToFront = true;
    [SerializeField] private int sortingOrderOnOpen = 1000;

    [Header("Animation Settings (แบบหน้าข่าว)")]
    [SerializeField] private bool animateOnOpen = true;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float slideDuration = 0.30f;
    [SerializeField] private float cardStartOffsetY = -400f;  // ลอยมาจากล่าง

    [Header("Events")]
    public UnityEvent<int> OnOpenIndex;
    public UnityEvent OnCloseAll;

    private int _current = -1;
    private bool _initialized;
    private bool _isAnimating = false;

    // เก็บตำแหน่งเป้าหมายของแต่ละ CanvasGroup (เหมือน card1TargetPos / card2TargetPos)
    private Vector2[] _targetPositions;

    // ---------- Unity lifecycle ----------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[OpenCanvas] Multiple instances found, keeping the first one.");
        }
        else
        {
            Instance = this;
        }

        TryAutoFillCanvases();
        CacheTargetPositions();
        TryAutoBindOwner();
        InitAllClosed();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        if (_isAnimating) return;
        if (!EnsureReady()) return;

        if (number < 0 || number >= canvases.Length)
        {
            Debug.LogWarning($"[OpenCanvas] index out of range: {number} (0..{canvases.Length - 1})");
            return;
        }

        bool reopen = (_current == number);

        StartCoroutine(ShowRoutine(number, reopen));
    }

    /// <summary>ปิดทุกแผง (ใช้กับปุ่ม Close)</summary>
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
        if (mapIdToIndex == null)
        {
            Debug.LogWarning("[OpenCanvas] mapIdToIndex is null");
            return;
        }
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
            CacheTargetPositions();
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

    private void CacheTargetPositions()
    {
        if (canvases == null) return;

        _targetPositions = new Vector2[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            var cg = canvases[i];
            if (!cg)
            {
                _targetPositions[i] = Vector2.zero;
                continue;
            }

            RectTransform rt = cg.GetComponent<RectTransform>();
            if (rt != null)
                _targetPositions[i] = rt.anchoredPosition;
            else
                _targetPositions[i] = Vector2.zero;
        }
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

    // ปิดทุกตัวแบบไม่อนิเมท (ใช้ตอนเริ่มเกม / รีเซ็ตภายใน)
    private void CloseAllImmediate()
    {
        if (canvases == null) return;

        foreach (var cg in canvases)
        {
            if (!cg) continue;

            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            if (cg.gameObject.activeSelf)
                cg.gameObject.SetActive(false);
        }
    }

    private string GetPath(Transform tr)
    {
        System.Text.StringBuilder sb = new();
        while (tr != null)
        {
            sb.Insert(0, "/" + tr.name);
            tr = tr.parent;
        }
        return sb.ToString();
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

    // ---------- Animation Routines (แบบหน้าข่าว) ----------

    private IEnumerator ShowRoutine(int index, bool reopen)
    {
        _isAnimating = true;
        _current = index;

        // ปิดตัวอื่น
        for (int i = 0; i < canvases.Length; i++)
        {
            if (i == index) continue;
            var cg = canvases[i];
            if (!cg) continue;

            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            if (cg.gameObject.activeSelf) cg.gameObject.SetActive(false);
        }

        var target = canvases[index];
        if (!target)
        {
            _isAnimating = false;
            yield break;
        }

        ForceEnableHierarchy(target.gameObject);

        if (blockRaycast) blockRaycast.SetActive(true);
        controllerNet?.SetUIOpen(true);
        if (pauseWithTimescale) Time.timeScale = 0f;

        target.gameObject.SetActive(true);
        target.interactable = true;
        target.blocksRaycasts = true;

        var rt = target.GetComponent<RectTransform>();
        Vector2 targetPos = _targetPositions != null && index < _targetPositions.Length
            ? _targetPositions[index]
            : (rt ? rt.anchoredPosition : Vector2.zero);

        if (rt && animateOnOpen)
        {
            // ตั้งต้นให้การ์ดอยู่ต่ำลง
            rt.anchoredPosition = targetPos + new Vector2(0f, cardStartOffsetY);
        }

        // เตรียม fade
        if (target != null)
        {
            target.alpha = 0f;
        }

        if (bringToFront)
        {
            var canvasesInParents = target.GetComponentsInParent<Canvas>(true);
            foreach (var c in canvasesInParents)
            {
                c.enabled = true;
                c.overrideSorting = true;
                if (c.sortingOrder < sortingOrderOnOpen)
                    c.sortingOrder = sortingOrderOnOpen;
            }
        }

        float t = 0f;

        // Fade-in + slide-up พร้อมกัน (เหมือน NewsPanel)
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
            float a = Mathf.Clamp01(t);

            if (target != null)
                target.alpha = a * fadeAlpha;

            float slideT = Mathf.Clamp01(t / Mathf.Max(slideDuration / Mathf.Max(fadeDuration, 0.01f), 0.01f));

            if (rt && animateOnOpen)
            {
                rt.anchoredPosition = Vector2.Lerp(
                    targetPos + new Vector2(0f, cardStartOffsetY),
                    targetPos,
                    slideT
                );
            }

            yield return null;
        }

        if (target != null)
        {
            target.alpha = fadeAlpha;
        }
        if (rt && animateOnOpen)
        {
            rt.anchoredPosition = targetPos;
        }

        OnOpenIndex?.Invoke(index);
        Debug.Log($"[OpenCanvas] {(reopen ? "REOPEN" : "OPEN")} index={index} path={GetPath(target.transform)}", target);

        _isAnimating = false;
    }

    private IEnumerator HideRoutine()
    {
        _isAnimating = true;

        if (_current < 0 || _current >= canvases.Length)
        {
            _isAnimating = false;
            yield break;
        }

        var target = canvases[_current];
        if (!target)
        {
            _isAnimating = false;
            yield break;
        }

        var rt = target.GetComponent<RectTransform>();
        Vector2 targetPos = _targetPositions != null && _current < _targetPositions.Length
            ? _targetPositions[_current]
            : (rt ? rt.anchoredPosition : Vector2.zero);

        float startAlpha = target.alpha;
        float t = 0f;

        Vector2 cardStart = rt ? rt.anchoredPosition : Vector2.zero;

        // Fade-out + slide-down
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
            float a = Mathf.Clamp01(1f - t);

            if (target != null)
                target.alpha = a * startAlpha;

            float slideT = Mathf.Clamp01(t / Mathf.Max(slideDuration / Mathf.Max(fadeDuration, 0.01f), 0.01f));

            if (rt && animateOnOpen)
            {
                rt.anchoredPosition = Vector2.Lerp(
                    cardStart,
                    targetPos + new Vector2(0f, cardStartOffsetY),
                    slideT
                );
            }

            yield return null;
        }

        if (target != null)
        {
            target.alpha = 0f;
            target.interactable = false;
            target.blocksRaycasts = false;
            target.gameObject.SetActive(false);
        }

        if (blockRaycast) blockRaycast.SetActive(false);
        controllerNet?.SetUIOpen(false);
        if (pauseWithTimescale) Time.timeScale = 1f;

        OnCloseAll?.Invoke();
        _current = -1;

        Debug.Log("[OpenCanvas] CLOSE ALL");

        _isAnimating = false;
    }

    private void ForceEnableHierarchy(GameObject go)
    {
        var t = go.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

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

    // เรียกจาก closeCanvas()
    private void CloseAllUI()
    {
        if (_isAnimating) return;

        if (!EnsureReady())
        {
            CloseAllImmediate();
            return;
        }

        // ถ้ามีหน้าที่เปิดอยู่ → อนิเมชันปิดแบบข่าว
        if (_current >= 0 && _current < canvases.Length && animateOnOpen)
        {
            StartCoroutine(HideRoutine());
        }
        else
        {
            // ถ้าไม่มีอะไรกำลังเปิด → ปิดทิ้งเลย
            CloseAllImmediate();

            if (blockRaycast) blockRaycast.SetActive(false);
            controllerNet?.SetUIOpen(false);
            if (pauseWithTimescale) Time.timeScale = 1f;

            OnCloseAll?.Invoke();
            _current = -1;

            Debug.Log("[OpenCanvas] CLOSE ALL (instant)");
        }
    }
}
