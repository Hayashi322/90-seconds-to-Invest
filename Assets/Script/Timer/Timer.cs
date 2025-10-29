using UnityEngine;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class Timer : NetworkBehaviour
{
    public static Timer Instance { get; private set; }
    private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // networked time state
    private NetworkVariable<double> startTime = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> roundCount = new(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<float> currentTime = new(30f, NetworkVariableReadPermission.Everyone);

    [Header("Optional Panels")]
    [SerializeField] private CanvasGroup[] introPanels;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Tax UI Panel (has TaxUI)")]
    [SerializeField] private GameObject taxPanel;

    // (คงไว้ถ้าต้องการรอก่อนเริ่มรอบถัดไป)
    [Header("Results Waiting Rule (unused now)")]
    [SerializeField] private int minConnectedPlayers = 2;
    [SerializeField] private float waitForClientsTimeout = 15f;

    private bool timeUpTriggered = false;

    public int Phase => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) % 3) + 1);
    public int Round => ((roundCount.Value <= 0) ? 1 : ((roundCount.Value - 1) / 3) + 1);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        roundCount.OnValueChanged += (_, __) => { UpdateRoundLabel(); ApplyIntroPanels(); };
        if (IsServer) StartCountdown();
        else { UpdateRoundLabel(); ApplyIntroPanels(); }
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
            countdownText.text = "X";
            timeUpTriggered = true;

            if (IsServer)
            {
                // ✅ ครบ 9 เฟสแล้ว → ไปหน้า GameOver พร้อมกัน (ไม่ผ่าน Results)
                if (roundCount.Value >= 9)
                {
                    // ใช้ GameResultManager ถ้ามีอยู่
                    if (GameResultManager.Instance)
                        GameResultManager.Instance.ProceedToGameOverServerRpc();
                    else
                        SceneGoToGameOverServerRpc(); // fallback ถ้าไม่มีตัวจัดการ
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
        roundCount.Value++;
        switch (Phase)
        {
            case 1: currentTime.Value = 90f; break;
            case 2: currentTime.Value = 60f; break;
            case 3: currentTime.Value = 30f; break;
        }
        startTime.Value = NetworkManager.Singleton.ServerTime.Time;

        if (Phase == 3) EnterPhase3ClientRpc();
        else ShowTaxUIClientRpc(false);

        UpdateRoundLabel();
        ApplyIntroPanels();
        Debug.Log($"⏱️ Round {Round}, Phase {Phase}, {currentTime.Value}s");
    }

    private void UpdateRoundLabel()
    {
        if (!roundText) return;
        roundText.text = $"{Round}                         {Phase}";
    }

    private void ApplyIntroPanels()
    {
        if (introPanels == null || introPanels.Length == 0) return;
        foreach (var cg in introPanels) { if (!cg) continue; cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false; }
        if (Phase == 1 && introPanels[0])
        {
            var c = introPanels[0];
            c.alpha = 1; c.blocksRaycasts = true; c.interactable = true;
            Invoke(nameof(CloseAllIntroPanels), 3f);
        }
    }
    public void CloseAllIntroPanels()
    {
        if (introPanels == null) return;
        foreach (var c in introPanels) { if (!c) continue; c.alpha = 0; c.blocksRaycasts = false; c.interactable = false; }
    }

    // ===== Client RPCs =====
    [ClientRpc]
    private void EnterPhase3ClientRpc()
    {
        if (TaxManager.Instance) TaxManager.Instance.CalculateTaxThisPhaseServerRpc();
        if (taxPanel) taxPanel.SetActive(true);
    }
    [ClientRpc] private void ShowTaxUIClientRpc(bool show) { if (taxPanel) taxPanel.SetActive(show); }

    // ===== Fallback: ไป GameOver โดยตรงจาก Timer (ถ้าไม่มี GameResultManager) =====
    [ServerRpc(RequireOwnership = false)]
    private void SceneGoToGameOverServerRpc()
    {
        if (!IsServer) return;
        // ใส่ชื่อฉาก GameOver ให้ตรงกับ Build Settings
        NetworkManager.SceneManager.LoadScene("GameOver", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
