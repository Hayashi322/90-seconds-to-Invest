using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerLawState : NetworkBehaviour
{
    public static readonly List<PlayerLawState> Instances = new List<PlayerLawState>();

    [Header("Casino / Crime State")]
    // ตอนนี้อยู่ในคาสิโนไหม
    public NetworkVariable<bool> IsInCasino = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // เคยทำผิดจากคาสิโนไหม (ตั้งแต่กด Roll ครั้งแรก)
    public NetworkVariable<bool> HasCrime = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // เวลาเลิกผิด (เฉพาะช่วงหลังออกจากคาสิโน)
    public NetworkVariable<float> CrimeExpireTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Jail State")]
    // กำลังติดคุกอยู่ไหม
    public NetworkVariable<bool> IsInJail = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // เวลาออกจากคุก
    public NetworkVariable<float> JailReleaseTime = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

    // helper หา PlayerLawState จาก clientId
    public static PlayerLawState FindByClientId(ulong clientId)
    {
        return Instances.Find(p => p.OwnerClientId == clientId);
    }

    // helper เช็กว่าตอนนี้ "ผิดกฎหมายคาสิโน" อยู่ไหม
    public bool IsCurrentlyCriminal(float serverTimeNow)
    {
        if (!HasCrime.Value) return false;

        // ยังอยู่ในคาสิโน = ผิดแน่นอน
        if (IsInCasino.Value) return true;

        // ออกจากคาสิโนแล้ว = ผิดจนกว่าจะเลย CrimeExpireTime
        return CrimeExpireTime.Value > 0f && serverTimeNow <= CrimeExpireTime.Value;
    }

    // ---------- คาสิโนเรียกใช้ ----------

    /// <summary>เข้า คาสิโน (เปิด UI คาสิโน)</summary>
    [ServerRpc]
    public void EnterCasinoServerRpc()
    {
        IsInCasino.Value = true;
        Debug.Log($"[Law] Client {OwnerClientId} entered casino.");
    }

    /// <summary>ออก คาสิโน (ปิด UI คาสิโน)</summary>
    [ServerRpc]
    public void ExitCasinoServerRpc()
    {
        IsInCasino.Value = false;

        float now = (float)NetworkManager.ServerTime.Time;

        if (HasCrime.Value)
        {
            CrimeExpireTime.Value = now + 10f; // ผิดต่ออีก 10 วิ
            Debug.Log($"[Law] Client {OwnerClientId} left casino. Crime lasts until {CrimeExpireTime.Value}");
        }
        else
        {
            CrimeExpireTime.Value = 0f;
            Debug.Log($"[Law] Client {OwnerClientId} left casino with no crime.");
        }
    }

    /// <summary>กด Roll → เริ่มทำผิด</summary>
    [ServerRpc]
    public void NotifyCasinoRollServerRpc()
    {
        HasCrime.Value = true;
        Debug.Log($"[Law] Client {OwnerClientId} rolled dice → now criminal while in casino.");
    }

    // ---------- เรียกจากสถานีตำรวจ (คนแจ้งจับ) ----------

    /// <summary>Client ที่เป็นคนแจ้งเรียก → แจ้งจับ targetClientId</summary>
    [ServerRpc]
    public void RequestReportPlayerServerRpc(ulong targetClientId)
    {
        if (!IsServer) return;
        if (LawManager.Instance == null)
        {
            Debug.LogError("[Law] LawManager not found in scene.");
            return;
        }

        Debug.Log($"[Law] Client {OwnerClientId} reports client {targetClientId}");
        LawManager.Instance.HandleReport(OwnerClientId, targetClientId);
    }

    // ---------- ฟังก์ชันส่งเข้าคุก (server) + เปิด/ปิด UI (client) ----------

    /// <summary>
    /// ให้ Server เรียกเพื่อส่ง RPC ไปหา "เจ้าของ" ตัวนี้คนเดียว
    /// </summary>
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

    /// <summary>
    /// เรียกบน client ฝั่ง "คนที่โดนจับ" เท่านั้น
    /// </summary>
    [ClientRpc]
    private void StartJailClientRpc(float duration, string reason, ClientRpcParams rpcParams = default)
    {
        // กันไว้เผื่อ RPC ถูกส่งเกินมาหลายเครื่อง
        if (!IsOwner) return;

        Debug.Log($"[Law] Start jail on client {OwnerClientId} for {duration} sec. Reason={reason}");

        // ✅ ปิด UI ทั้งหมดของคนที่โดนจับ (คาสิโน / ตำรวจ / อื่น ๆ)
        if (OpenCanvas.Instance != null)
        {
            OpenCanvas.Instance.closeCanvas();
        }

        // TODO: ตรงนี้เอาไว้ต่อกับ JailPanel จริง ๆ ในสเต็ปถัดไป
        // เช่น: JailPanel.Instance.Show(this, duration, reason);
    }
    [ClientRpc]
    private void ShowJailUIClientRpc(float duration, string reason, ClientRpcParams clientRpcParams = default)
    {
        // เรียก UI ฝั่ง Client
        JialCanvas.Instance.Show(duration, reason);
    }
    public void ServerSendJailToOwner01(float duration, string reason)
    {
        // ส่ง RPC ไปเฉพาะเจ้าของคนนี้เท่านั้น
        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong> { OwnerClientId }
            }
        };

        ShowJailUIClientRpc(duration, reason, sendParams);
    }

}
