using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLawState : NetworkBehaviour
{
    public static readonly List<PlayerLawState> Instances = new List<PlayerLawState>();

    [Header("Casino / Crime State (legacy from old system)")]
    public NetworkVariable<bool> IsInCasino = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> HasCrime = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> CrimeExpireTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Jail State (ใช้กับ Education Media Tower)")]
    public NetworkVariable<bool> IsInJail = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> JailReleaseTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Education Media Tower State")]
    // จำว่าใช้ตึกนี้ครั้งล่าสุดใน PhaseIndex ไหน (1–9), เริ่มต้น = -1 = ยังไม่เคยใช้
    public NetworkVariable<int> LastMediaPhaseUsed = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ---------- life cycle ----------

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (!Instances.Contains(this))
                Instances.Add(this);

            Debug.Log($"[Law] Register PlayerLawState for client {OwnerClientId}. Total={Instances.Count}");
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            Instances.Remove(this);
            Debug.Log($"[Law] Remove PlayerLawState for client {OwnerClientId}. Total={Instances.Count}");
        }
    }

    public static PlayerLawState FindByClientId(ulong clientId)
    {
        return Instances.Find(p => p.OwnerClientId == clientId);
    }

    public bool IsCurrentlyCriminal(float serverTimeNow)
    {
        if (!HasCrime.Value) return false;
        if (IsInCasino.Value) return true;
        return CrimeExpireTime.Value > 0f && serverTimeNow <= CrimeExpireTime.Value;
    }

    // ---------- คาสิโน (ถ้ายังใช้บางส่วน) ----------

    [ServerRpc]
    public void EnterCasinoServerRpc()
    {
        IsInCasino.Value = true;
        Debug.Log($"[Law] Client {OwnerClientId} entered casino.");
    }

    [ServerRpc]
    public void ExitCasinoServerRpc()
    {
        IsInCasino.Value = false;

        float now = (float)NetworkManager.ServerTime.Time;

        if (HasCrime.Value)
        {
            CrimeExpireTime.Value = now + 10f;
            Debug.Log($"[Law] Client {OwnerClientId} left casino. Crime lasts until {CrimeExpireTime.Value}");
        }
        else
        {
            CrimeExpireTime.Value = 0f;
            Debug.Log($"[Law] Client {OwnerClientId} left casino with no crime.");
        }
    }

    [ServerRpc]
    public void NotifyCasinoRollServerRpc()
    {
        HasCrime.Value = true;
        Debug.Log($"[Law] Client {OwnerClientId} rolled dice → now criminal while in casino.");
    }

    // ---------- Education Media Tower (กดไอคอนเลือกเป้าหมาย) ----------

    /// <summary>
    /// Client ที่เป็นคนใช้งาน Education Media Tower เรียก
    /// → Server เช็กเฟส + เงิน 10,000 + กดได้เฟสละครั้ง
    /// → ถ้าผ่านค่อยส่งไป LawManager สุ่ม 40/60
    /// </summary>
    [ServerRpc]
    public void RequestReportPlayerServerRpc(ulong targetClientId)
    {
        if (!IsServer) return;

        if (LawManager.Instance == null)
        {
            Debug.LogError("[Law] LawManager not found in scene.");
            return;
        }

        // 1) ดึง Round/Phase ปัจจุบันจาก Timer
        if (Timer.Instance == null)
        {
            Debug.LogWarning("[Law] Timer.Instance is null. Cannot determine phase.");
            return;
        }

        int round = Timer.Instance.Round; // 1–3
        int phase = Timer.Instance.Phase; // 1–3

        // แปลงเป็นเลข PhaseIndex ทั้งเกม (1..9)
        int currentPhaseIndex = (round - 1) * 3 + phase;

        // 2) เช็กว่าเฟสนี้เคยใช้ไปแล้วหรือยัง
        if (LastMediaPhaseUsed.Value == currentPhaseIndex)
        {
            Debug.Log($"[Law] Client {OwnerClientId} already used MediaTower in phaseIndex {currentPhaseIndex}.");
            return;
        }

        // 3) หาตัว InventoryManager ของ player คนนี้บน Server
        var inv = GetComponent<InventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[Law] InventoryManager not found on player object.");
            return;
        }

        const int cost = 10_000;
        bool paid = inv.TrySpendCash(cost);
        if (!paid)
        {
            Debug.Log($"[Law] Client {OwnerClientId} does not have enough cash to use MediaTower.");
            return;
        }

        // 4) ผ่านทุกเงื่อนไข → ยอมให้ใช้ใน Phase นี้ และจำว่าใช้ PhaseIndex ไหนไปแล้ว
        LastMediaPhaseUsed.Value = currentPhaseIndex;

        Debug.Log($"[Law] Client {OwnerClientId} uses EducationMediaTower on client {targetClientId} at phaseIndex {currentPhaseIndex}.");
        LawManager.Instance.HandleReport(OwnerClientId, targetClientId);
    }

    // ---------- ส่งเข้าคุก / เรียก UI ขัง ----------

    public void ServerSendJailToOwner(float duration, string reason)
    {
        if (!IsServer) return;

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };

        ShowJailUIClientRpc(duration, reason, sendParams);
    }

    [ClientRpc]
    private void ShowJailUIClientRpc(float duration, string reason, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;

        Debug.Log($"[Law] Start jail on client {OwnerClientId} for {duration} sec. Reason={reason}");

        if (OpenCanvas.Instance != null)
        {
            OpenCanvas.Instance.closeCanvas();
        }

        if (JialCanvas.Instance != null)
        {
            JialCanvas.Instance.Show(duration, reason);
        }
        else
        {
            Debug.LogWarning("[Law] JialCanvas.Instance is null. Cannot show jail UI.");
        }
    }
}
