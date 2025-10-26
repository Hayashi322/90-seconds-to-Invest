using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Timer : NetworkBehaviour
{
    // ---------- Singleton ----------
    public static Timer Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Networked state ----------
    private NetworkVariable<double> startTime = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> roundCount = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<float> currentTime = new(30f, NetworkVariableReadPermission.Everyone);

    [Header("UI")]
    [SerializeField] private CanvasGroup[] canvases;   // optional: panel switching
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Tax UI (panel with TaxUI)")]
    [SerializeField] private GameObject taxPanel;

    private bool timeUpTriggered = false;

    // ---------- Helpers ----------
    public int Phase => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) % 3) + 1);
    public int Round => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) / 3) + 1);
    public bool IsPhase2 => (Phase == 2);  // ใช้เช็คใน UI อื่น ๆ

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // sync UI เมื่อรอบ/เฟสเปลี่ยน
        roundCount.OnValueChanged += (_, __) =>
        {
            ApplyPhaseUI();
            UpdateRoundLabel();
        };

        if (IsServer) StartCountdown(); // เริ่มรอบแรกที่เซิร์ฟเวอร์
        else
        {
            ApplyPhaseUI();
            UpdateRoundLabel();
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

            if (IsServer)
                Invoke(nameof(StartCountdown), 2f);   // เริ่มรอบใหม่หลัง 2 วินาที
        }
    }

    // ---------- Server: advance round/phase ----------
    private void StartCountdown()
    {
        roundCount.Value++;

        // ความยาวเวลาแต่ละสเต็ป (ตามลอจิกเดิม)
        switch (roundCount.Value)
        {
            case 1: currentTime.Value = 60f; break; // R1 P1
            case 2: currentTime.Value = 10f; break; // R1 P2
            case 3: currentTime.Value = 10f; break; // R1 P3
            case 4: currentTime.Value = 30f; break; // R2 P1 (เปิด canvases[0])
            case 5: currentTime.Value = 10f; break; // R2 P2
            case 6: currentTime.Value = 10f; break; // R2 P3
            case 7: currentTime.Value = 15f; break; // R3 P1 (เปิด canvases[1])
            case 8: currentTime.Value = 10f; break; // R3 P2
            case 9: currentTime.Value = 10f; break; // R3 P3
            default:
                currentTime.Value = 99999f;          // กันวน
                break;
        }

        startTime.Value = NetworkManager.Singleton.ServerTime.Time;

        // แจ้งทุกคนให้อัปเดต UI
        ApplyPhaseUI();
        UpdateRoundLabel();

        // จัดการหน้าภาษี: โชว์เฉพาะ Phase 2 (รหัสรอบ 2,5,8)
        bool enterPhase2 = (roundCount.Value == 2 || roundCount.Value == 5 || roundCount.Value == 8);
        if (enterPhase2) EnterPhase2ClientRpc();
        else ShowTaxUIClientRpc(false);

        Debug.Log($"⏱️ Start Round {Round} Phase {Phase} with {currentTime.Value} seconds");
    }

    // ---------- Client RPC ----------
    [ClientRpc]
    private void EnterPhase2ClientRpc()
    {
        // ให้ client ยิงคำนวณภาษีของตัวเอง
        if (TaxManager.Instance) TaxManager.Instance.CalculateTaxServerRpc();

        // เปิดหน้าภาษี
        if (taxPanel) taxPanel.SetActive(true);
    }

    [ClientRpc]
    private void ShowTaxUIClientRpc(bool show)
    {
        if (taxPanel) taxPanel.SetActive(show);
    }

    // ---------- UI helpers ----------
    private void ApplyPhaseUI()
    {
        if (canvases == null || canvases.Length == 0) return;

        // reset
        foreach (var c in canvases)
        {
            if (!c) continue;
            c.alpha = 0f; c.blocksRaycasts = false; c.interactable = false;
        }

        // ตัวอย่าง: R2 P1 -> canvases[0], R3 P1 -> canvases[1]
        if (Round == 2 && Phase == 1 && canvases.Length >= 1 && canvases[0])
        {
            canvases[0].alpha = 1f; canvases[0].blocksRaycasts = true; canvases[0].interactable = true;
        }
        else if (Round == 3 && Phase == 1 && canvases.Length >= 2 && canvases[1])
        {
            canvases[1].alpha = 1f; canvases[1].blocksRaycasts = true; canvases[1].interactable = true;
        }
    }

    private void UpdateRoundLabel()
    {
        if (!roundText) return;
        if (roundCount.Value >= 10) roundText.text = "The END";
        else roundText.text = $"Round:{Round}  Phase:{Phase}";
    }

    public void CloseAllPanels()
    {
        if (canvases == null) return;
        foreach (var c in canvases)
        {
            if (!c) continue;
            c.alpha = 0f; c.blocksRaycasts = false; c.interactable = false;
        }
        if (taxPanel) taxPanel.SetActive(false);
    }
}
