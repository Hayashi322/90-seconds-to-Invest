using System.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JailUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image playerIcon; // ถ้ามี avatar

    private PlayerLawState localLaw;

    private void OnEnable()
    {
        if (localLaw == null)
        {
            StartCoroutine(BindLocalLawState());
        }
    }

    private IEnumerator BindLocalLawState()
    {
        while (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            yield return null;

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        while (localObj == null)
        {
            localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            yield return null;
        }

        localLaw = localObj.GetComponent<PlayerLawState>();
    }

    private void Update()
    {
        if (localLaw == null)
        {
            root.SetActive(false);
            return;
        }

        if (localLaw.IsInJail.Value)
        {
            root.SetActive(true);

            float now = (float)NetworkManager.Singleton.ServerTime.Time;
            float remain = Mathf.Max(0, localLaw.JailReleaseTime.Value - now);

            if (countdownText)
                countdownText.text = remain.ToString("0");

            // playerIcon.sprite → ถ้ามีระบบ avatar ก็เซ็ตที่นี่
        }
        else
        {
            root.SetActive(false);
        }
    }
}
