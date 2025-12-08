using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PoliceStationUI : MonoBehaviour
{
    [Header("Root Panel (ตัว Panel หลักของสถานีตำรวจ)")]
    [SerializeField] private GameObject root;

    [Header("Suspect List (Right Side)")]
    [SerializeField] private Transform suspectListRoot;
    [SerializeField] private SuspectButton suspectButtonPrefab;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI infoText;

    private PlayerLawState localLaw;

    private void OnEnable()
    {
        // root ไม่ต้องไปปิด/เปิดเองมากนัก ให้ OpenCanvas จัดการเป็นหลัก
        if (root == null) root = gameObject;

        if (titleText)
            titleText.text = "สถานีตำรวจ";

        StartCoroutine(BindLocalLawAndRefresh());
    }

    private IEnumerator BindLocalLawAndRefresh()
    {
        // รอ Netcode พร้อม
        while (NetworkManager.Singleton == null ||
               NetworkManager.Singleton.SpawnManager == null)
            yield return null;

        var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        while (localObj == null)
        {
            localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            yield return null;
        }

        localLaw = localObj.GetComponent<PlayerLawState>();
        if (localLaw == null)
        {
            Debug.LogError("[PoliceUI] Local player has no PlayerLawState.");
            yield break;
        }

        RefreshSuspectList();
    }

    /// <summary>
    /// ปิดหน้าต่างสถานีตำรวจ:
    /// - ไม่ปิด root แล้ว (ไม่ SetActive(false) ลูก)
    /// - ให้ OpenCanvas.closeCanvas() เป็นคนจัดการปิด CanvasGroup + blockRaycast
    /// - สั่ง hero.SetUIOpen(false) กันเหนียว
    /// </summary>
    public void Close()
    {
        // ❌ อย่าปิด root ตรง ๆ เดี๋ยวมันไม่ตื่นอีก
        // if (root != null)
        //     root.SetActive(false);

        // 1) ให้ OpenCanvas ปิดแคนวาส/บล็อกต่าง ๆ
        var oc = FindObjectOfType<OpenCanvas>(true);
        if (oc != null)
        {
            oc.closeCanvas();
        }

        // 2) กันเหนียว: สั่งปลด PauseByUI ผ่าน HeroControllerNet
        var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj != null)
        {
            var hero = localObj.GetComponent<HeroControllerNet>();
            if (hero != null)
            {
                hero.SetUIOpen(false);
            }
        }
    }

    private void RefreshSuspectList()
    {
        if (suspectListRoot == null || suspectButtonPrefab == null)
        {
            Debug.LogError("[PoliceUI] suspectListRoot or suspectButtonPrefab not assigned!");
            return;
        }

        foreach (Transform child in suspectListRoot)
            Destroy(child.gameObject);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[PoliceUI] No NetworkManager.");
            return;
        }

        int created = 0;

        // แสดงผู้เล่นทุกคนยกเว้นตัวเอง
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var p = playerObj.GetComponent<PlayerLawState>();
            if (p == null) continue;
            if (!p.IsSpawned) continue;

            if (localLaw != null && p.OwnerClientId == localLaw.OwnerClientId)
                continue;

            var btn = Instantiate(suspectButtonPrefab, suspectListRoot);
            btn.Setup(p, OnSelectSuspect);
            created++;
        }

        if (infoText)
        {
            if (created == 0)
            {
                infoText.text = "ไม่สามารถใช้สถานที่นี่ได้ (ไม่มีผู้เล่นคนอื่นในเมือง)";
                StartCoroutine(AutoCloseShortly());
            }
            else
            {
                infoText.text = "เลือกไอคอนผู้เล่นที่ต้องการเชิญไปออกรายการ";
            }
        }
    }

    private IEnumerator AutoCloseShortly()
    {
        yield return new WaitForSeconds(2f);
        Close();
    }

    private void OnSelectSuspect(PlayerLawState suspect)
    {
        if (localLaw != null)
        {
            localLaw.RequestReportPlayerServerRpc(suspect.OwnerClientId);
        }
        Close();
    }
}
