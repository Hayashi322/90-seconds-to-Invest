using UnityEngine;
using Unity.Netcode;

public class PlayerSpawnDebugger : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[SpawnDBG] NetworkManager.Singleton is null");
            return;
        }

        // แสดงจำนวน client ไว้ดูเฉย ๆ
        Debug.Log($"[SpawnDBG] OnNetworkSpawn ({UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}) | Clients = {nm.ConnectedClients.Count}");

        // ทำเฉพาะฝั่ง Host / Server ก็พอ
        if (!nm.IsServer)
            return;

        // ถ้า host มี PlayerObject อยู่แล้วก็ไม่ต้องทำอะไร
        if (nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
        {
            Debug.Log("[SpawnDBG] Host already has PlayerObject.");
            return;
        }

        // ===== ใช้ NetworkConfig.PlayerPrefab แทน nm.PlayerPrefab =====
        var playerPrefab = nm.NetworkConfig.PlayerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnDBG] NetworkConfig.PlayerPrefab is null. ไปตั้งค่าที่ NetworkManager -> NetworkConfig ก่อน");
            return;
        }

        // สร้าง player prefab แล้ว Spawn เป็น PlayerObject ของ host
        var obj = Instantiate(
            playerPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[SpawnDBG] PlayerPrefab ไม่มี NetworkObject ติดอยู่");
            Destroy(obj);
            return;
        }

        netObj.SpawnAsPlayerObject(nm.LocalClientId, true);
        Debug.Log("[SpawnDBG] Host player spawned manually.");
    }
}
