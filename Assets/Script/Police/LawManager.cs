using Unity.Netcode;
using UnityEngine;

public class LawManager : NetworkBehaviour
{
    public static LawManager Instance { get; private set; }

    [Header("Media Show / Jail Settings")]
    [SerializeField] private Transform jailPoint;     // จุดย้ายไปตอนถูกเรียกไปออกรายการ
    [SerializeField] private Transform spawnPoint;    // จุดกลับเมืองหลังพ้นเวลา

    [SerializeField] private float accuserJailDuration = 5f;    // เวลาโดนขังของคนกดเอง
    [SerializeField] private float targetJailDuration = 15f;    // เวลาโดนขังของเป้าหมาย
    [SerializeField, Range(0f, 1f)] private float accuserChance = 0.4f; // โอกาสที่คนกดจะโดนเอง (40%)

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsServer) return;

        float now = (float)NetworkManager.ServerTime.Time;

        foreach (var p in PlayerLawState.Instances)
        {
            if (p == null || !p.IsSpawned) continue;

            // เช็กหมดเวลาขัง
            if (p.IsInJail.Value && now >= p.JailReleaseTime.Value)
            {
                ReleaseFromJail(p);
            }
        }
    }

    /// <summary>
    /// ถูกเรียกเมื่อผู้เล่นใช้งาน Education Media Tower:
    /// accuser = คนกดใช้ตึก, target = เป้าหมายที่ถูกเลือกใน UI
    /// สุ่ม 40/60 ว่าใครจะถูกเรียกไปออกรายการ
    /// </summary>
    public void HandleReport(ulong accuserClientId, ulong targetClientId)
    {
        var accuser = PlayerLawState.Instances.Find(x => x.OwnerClientId == accuserClientId);
        var target = PlayerLawState.Instances.Find(x => x.OwnerClientId == targetClientId);

        if (accuser == null || target == null) return;

        float rand = UnityEngine.Random.value;

        if (rand < accuserChance)
        {
            // 40% คนกดโดนเอง → ขังสั้น 5 วินาที (ทีมงานเรียกชื่อผิด)
            SendToJail(accuser, accuserJailDuration, "ทีมงานส่งชื่อคุณผิด ต้องไปออกรายการสั้น ๆ");
            Debug.Log($"[Law] Accuser {accuserClientId} got called instead. Jailed for {accuserJailDuration} sec.");
        }
        else
        {
            // 60% เป้าหมายโดนขัง → 15 วินาที
            SendToJail(target, targetJailDuration, "ถูกเชิญไปออกรายการให้ความรู้การลงทุน");
            Debug.Log($"[Law] Target {targetClientId} sent to show. Jailed for {targetJailDuration} sec.");
        }
    }

    private void SendToJail(PlayerLawState player, float duration, string reason)
    {
        if (player == null) return;

        float now = (float)NetworkManager.ServerTime.Time;

        player.IsInJail.Value = true;
        player.JailReleaseTime.Value = now + duration;

        // ล้างสถานะ crime เก่า (จากระบบคาสิโนเดิม เผื่อยังมีค่าอยู่)
        player.HasCrime.Value = false;
        player.CrimeExpireTime.Value = 0f;
        player.IsInCasino.Value = false;

        // เทเลพอร์ตไปจุดคุก/สตูดิโอออกรายการ
        TeleportAndReset(player, jailPoint);

        // แจ้งไปยัง client ฝั่งเจ้าของให้เปิด UI นับเวลา
        player.ServerSendJailToOwner(duration, reason);
    }

    private void ReleaseFromJail(PlayerLawState player)
    {
        if (player == null) return;

        player.IsInJail.Value = false;
        player.JailReleaseTime.Value = 0f;

        // เทเลพอร์ตกลับ spawn
        TeleportAndReset(player, spawnPoint);
    }

    private void TeleportAndReset(PlayerLawState player, Transform target)
    {
        if (player == null || player.NetworkObject == null) return;

        if (target != null)
            player.NetworkObject.transform.position = target.position;

        var hero = player.GetComponent<HeroControllerNet>();
        if (hero)
        {
            hero.ServerResetPathAfterTeleport();
            hero.PauseByUI.Value = false;
        }
    }
}
