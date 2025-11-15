using Unity.Netcode;
using UnityEngine;

public class LawManager : NetworkBehaviour
{
    public static LawManager Instance { get; private set; }

    [Header("Jail Settings")]
    [SerializeField] private Transform jailPoint;
    [SerializeField] private float jailDuration = 10f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;

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

            // หมดเวลา crime
            if (p.HasCrime.Value &&
                !p.IsInCasino.Value &&
                p.CrimeExpireTime.Value > 0f &&
                now >= p.CrimeExpireTime.Value)
            {
                p.HasCrime.Value = false;
                p.CrimeExpireTime.Value = 0f;
            }

            // หมดเวลา jail
            if (p.IsInJail.Value && now >= p.JailReleaseTime.Value)
            {
                ReleaseFromJail(p);
            }
        }
    }

    public void HandleReport(ulong accuserClientId, ulong targetClientId)
    {
        var accuser = PlayerLawState.Instances.Find(x => x.OwnerClientId == accuserClientId);
        var target = PlayerLawState.Instances.Find(x => x.OwnerClientId == targetClientId);

        if (accuser == null || target == null) return;

        float now = (float)NetworkManager.ServerTime.Time;

        bool targetIsCriminal =
            target.HasCrime.Value &&
            (target.IsInCasino.Value ||
            (target.CrimeExpireTime.Value > 0f && now <= target.CrimeExpireTime.Value));

        if (targetIsCriminal)
        {
            // ผู้ต้องหามีความผิด → ขังผู้ต้องหา
            SendToJail(target, "เล่นคาสิโนผิดกฎหมาย");
            Debug.Log($"[Law] Target {targetClientId} guilty. Jailed for {jailDuration} sec.");
        }
        else
        {
            // แจ้งความเท็จ → ขังคนแจ้ง
            SendToJail(accuser, "แจ้งความเท็จ");
            Debug.Log($"[Law] Accuser {accuserClientId} made false report. Jailed for {jailDuration} sec.");
        }
    }

    private void SendToJail(PlayerLawState player, string reason)
    {
        if (player == null) return;

        float now = (float)NetworkManager.ServerTime.Time;

        player.IsInJail.Value = true;
        player.JailReleaseTime.Value = now + jailDuration;

        // ล้างสถานะ crime
        player.HasCrime.Value = false;
        player.CrimeExpireTime.Value = 0f;
        player.IsInCasino.Value = false;

        // เทเลพอร์ตไปจุดคุก
        TeleportAndReset(player, jailPoint);

        // ✅ สั่งให้ client ฝั่ง "เจ้าของคนนี้" ปิด UI + เปิดหน้า Jail (ถ้ามี)
        player.ServerSendJailToOwner(jailDuration, reason);
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
            // เคลียร์ path กับ UI pause เพื่อให้เดินได้ 100%
            hero.ServerResetPathAfterTeleport();
            hero.PauseByUI.Value = false;
        }
    }
}
