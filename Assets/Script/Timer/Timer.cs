using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Timer : NetworkBehaviour
{
    public static Timer Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // สถานะเวลาแบบเน็ตเวิร์ก
    private NetworkVariable<double> startTime = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> roundCount = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<float> currentTime = new(30f, NetworkVariableReadPermission.Everyone);

    [Header("Optional Panels (Phase 1 Intro)")]
    [SerializeField] private CanvasGroup[] introPanels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Tax UI Panel (has TaxUI)")]
    [SerializeField] private GameObject taxPanel;

    private bool timeUpTriggered = false;

    public int Phase => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) % 3) + 1);
    public int Round => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) / 3) + 1);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        roundCount.OnValueChanged += (_, __) =>
        {
            UpdateRoundLabel();
            ApplyIntroPanels();
        };

        if (IsServer) StartCountdown();
        else
        {
            UpdateRoundLabel();
            ApplyIntroPanels();
        }
    }

    private void Update()
    {
        if (!countdownText) return;

        double elapsed = NetworkManager.Singleton.ServerTime.Time - startTime.Value;
        float timeLeft = Mathf.Max(0f, currentTime.Value - (float)elapsed);

        if (timeLeft > 0f)
        {
            countdownText.text = timeLeft.ToString("F0");
            timeUpTriggered = false;
        }
        else if (!timeUpTriggered)
        {
            countdownText.text = "Time's up!";
            timeUpTriggered = true;
            if (IsServer) Invoke(nameof(StartCountdown), 2f);
        }
    }

    private void StartCountdown()
    {
        roundCount.Value++;

        // ตัวอย่างความยาวต่อเฟส
        switch (Phase)
        {
            case 1: currentTime.Value = 90f; break; // วางแผน/สะสม
            case 2: currentTime.Value = 60f; break; // เหตุการณ์ตลาด
            case 3: currentTime.Value = 30f; break; // สรุป/ชำระภาษี
        }

        startTime.Value = NetworkManager.Singleton.ServerTime.Time;

        // Phase 3 → ให้ทุก client คำนวณบิลภาษี + เปิดหน้าภาษี
        if (Phase == 3) EnterPhase3ClientRpc();
        else ShowTaxUIClientRpc(false);

        UpdateRoundLabel();
        ApplyIntroPanels();
        Debug.Log($"⏱️ Round {Round}, Phase {Phase}, {currentTime.Value}s");
    }

    private void UpdateRoundLabel()
    {
        if (!roundText) return;
        roundText.text = $"{Round}                        {Phase}";
    }

    private void ApplyIntroPanels()
    {
        if (introPanels == null || introPanels.Length == 0) return;

        foreach (var c in introPanels)
        {
            if (!c) continue;
            c.alpha = 0f; c.blocksRaycasts = false; c.interactable = false;
        }

        if (Phase == 1 && introPanels[0])
        {
            var cg = introPanels[0];
            cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true;
            Invoke(nameof(CloseAllIntroPanels), 3f);
        }
    }

    public void CloseAllIntroPanels()
    {
        if (introPanels == null) return;
        foreach (var c in introPanels)
        {
            if (!c) continue;
            c.alpha = 0f; c.blocksRaycasts = false; c.interactable = false;
        }
    }

    // ===== Client RPCs =====
    [ClientRpc]
    private void EnterPhase3ClientRpc()
    {
        if (TaxManager.Instance) TaxManager.Instance.CalculateTaxThisPhaseServerRpc();
        if (taxPanel) taxPanel.SetActive(true);
    }

    [ClientRpc]
    private void ShowTaxUIClientRpc(bool show)
    {
        if (taxPanel) taxPanel.SetActive(show);
    }
}
