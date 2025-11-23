using UnityEngine;
using TMPro;
using Unity.Netcode;

public class Timer : NetworkBehaviour
{
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

    // ===== Config =====
    // ตอนเทสต์: จบเกมเมื่อครบ 1 phase
    // ถ้าเล่นจริงครบ 3 รอบ (9 phase) → เปลี่ยนเป็น 9
    private const int EndPhaseCount = 1;

    // ===== Networked State =====
    private NetworkVariable<double> startTime =
        new NetworkVariable<double>(0, NetworkVariableReadPermission.Everyone);

    private NetworkVariable<int> roundCount =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    private NetworkVariable<float> currentTime =
        new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone);

    [Header("Optional Panels")]
    [SerializeField] private CanvasGroup[] introPanels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Round Canvas")]
    [SerializeField] private TextMeshProUGUI RoundText;
    [SerializeField] private TextMeshProUGUI PhaseText;

    [Header("Tax UI Panel (has TaxUI)")]
    [SerializeField] private GameObject taxPanel;

    private bool timeUpTriggered = false;

    // roundCount = นับทุก Phase
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

        if (IsServer)
        {
            StartCountdown();
        }
        else
        {
            // ฝั่ง Client แค่ sync UI ตามค่าที่มีอยู่
            UpdateRoundLabel();
            ApplyIntroPanels();
        }
    }

    private void Update()
    {
        if (!countdownText || NetworkManager.Singleton == null) return;

        double elapsed = NetworkManager.Singleton.ServerTime.Time - startTime.Value;
        float timeLeft = Mathf.Max(0f, currentTime.Value - (float)elapsed);

        if (timeLeft > 0f)
        {
            countdownText.text = timeLeft.ToString("F0");
            timeUpTriggered = false;
        }
        else if (!timeUpTriggered)
        {
            countdownText.text = "X";
            timeUpTriggered = true;

            if (IsServer)
            {
                Debug.Log($"[Timer] TimeUp → roundCount={roundCount.Value}, checking end condition...");

                if (roundCount.Value >= EndPhaseCount)
                {
                    Debug.Log($"[Timer] Final phase reached (roundCount={roundCount.Value}), request GameOver.");

                    if (GameResultManager.Instance != null)
                    {
                        GameResultManager.Instance.RequestGameOver();
                    }
                    else
                    {
                        Debug.LogWarning("[Timer] GameResultManager.Instance == null → fallback load GameOver.");
                        ForceLoadGameOver();
                    }
                }
                else
                {
                    Invoke(nameof(StartCountdown), 2f);
                }
            }
        }
    }

    private void StartCountdown()
    {
        if (!IsServer) return; // ป้องกัน client เรียก

        roundCount.Value++;

        switch (Phase)
        {
            case 1: currentTime.Value = 90f; break;
            case 2: currentTime.Value = 60f; break;
            case 3: currentTime.Value = 30f; break;
        }

        startTime.Value = NetworkManager.Singleton.ServerTime.Time;

        if (Phase == 3)
            EnterPhase3ClientRpc();
        else
            ShowTaxUIClientRpc(false);

        UpdateRoundLabel();
        ApplyIntroPanels();

        Debug.Log($"⏱️ Round {Round}, Phase {Phase}, {currentTime.Value}s (roundCount={roundCount.Value})");
    }

    private void UpdateRoundLabel()
    {
        if (!roundText) return;
        roundText.text = $"{Round}                         {Phase}";
    }

    private void ApplyIntroPanels()
    {
        if (introPanels == null || introPanels.Length == 0) return;

        foreach (var cg in introPanels)
        {
            if (!cg) continue;
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        // ไม่โชว์ intro ตอน Round1 Phase1
        if (Phase == 1 && Round == 1 && introPanels[0])
            return;

        var c = introPanels[0];
        c.alpha = 1;
        c.blocksRaycasts = true;
        c.interactable = true;

        if (RoundText) RoundText.text = Round.ToString();
        if (PhaseText) PhaseText.text = Phase.ToString();

        Invoke(nameof(CloseAllIntroPanels), 3f);
    }

    public void CloseAllIntroPanels()
    {
        if (introPanels == null) return;

        foreach (var c in introPanels)
        {
            if (!c) continue;
            c.alpha = 0;
            c.blocksRaycasts = false;
            c.interactable = false;
        }
    }

    // ===== Client RPCs =====
    [ClientRpc]
    private void EnterPhase3ClientRpc()
    {
        if (TaxManager.Instance)
            TaxManager.Instance.CalculateTaxThisPhaseServerRpc();

        if (taxPanel)
            taxPanel.SetActive(true);
    }

    [ClientRpc]
    private void ShowTaxUIClientRpc(bool show)
    {
        if (taxPanel)
            taxPanel.SetActive(show);
    }

    // ใช้ตอน GameResultManager หาย (กันพัง)
    private void ForceLoadGameOver()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
        {
            nm.SceneManager.LoadScene(
                "GameOver",
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
        }
    }
}
