using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class Timer : NetworkBehaviour
{
    private NetworkVariable<double> startTime = new NetworkVariable<double>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<int> roundCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private NetworkVariable<float> currentDuration = new NetworkVariable<float>(30f, NetworkVariableReadPermission.Everyone);
    // private NetworkVariable<CanvasGroup[]> canvases = new NetworkVariable<CanvasGroup[]>( NetworkVariableReadPermission.Everyone);
    [SerializeField] private CanvasGroup[] canvases;
    public TextMeshProUGUI countdownText;

    private bool timeUpTriggered = false;

    //  [SerializeField] private CanvasGroup[] canvases;
    // private float currentDuration = 30f;

    void Start()
    {

        if (IsServer)
        {
            StartCountdown(); // เริ่มรอบแรก
        }
    }

    void Update()
    {
        if (countdownText == null) return;


        double elapsed = NetworkManager.Singleton.ServerTime.Time - startTime.Value;
        float timeLeft = Mathf.Max(0, currentDuration.Value - (float)elapsed);

        if (timeLeft > 0)
        {
            countdownText.text = timeLeft.ToString("F0");
            timeUpTriggered = false;
        }
        else
        {
            if (!timeUpTriggered)
            {
                countdownText.text = "Time's up!";
                timeUpTriggered = true;

                if (IsServer)
                {
                    Invoke(nameof(StartCountdown), 2f); // เริ่มรอบใหม่หลัง 2 วิ
                }
            }
        }
    }

    private void StartCountdown()
    {
        roundCount.Value++;

        // เปลี่ยนเวลาตามรอบ
        if (roundCount.Value == 1)
        {
            currentDuration.Value = 15f;
            
        }
        else if (roundCount.Value == 2)
        {
            currentDuration.Value = 30f;
            canvases[0].alpha = 1; canvases[0].blocksRaycasts = true; canvases[0].interactable = true;
        }
        else
        {
            currentDuration.Value = 15f;
            canvases[0].alpha = 0; canvases[0].blocksRaycasts = false; canvases[0].interactable = false;
            canvases[1].alpha = 1; canvases[1].blocksRaycasts = true; canvases[1].interactable = true;
        }

        startTime.Value = NetworkManager.Singleton.ServerTime.Time;
        Debug.Log($"⏱️ Start Round {roundCount.Value} with {currentDuration} seconds");
    }

}

