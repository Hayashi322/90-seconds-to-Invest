using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSyncProbe : MonoBehaviour
{
    void OnEnable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // ฟังทุก scene event (เริ่มโหลด, โหลดเสร็จต่อ client, sync ฯลฯ)
        nm.SceneManager.OnSceneEvent += HandleSceneEvent;

        // สรุปตอนจบการโหลด (ได้ลิสต์ OK/TIMEOUT)
        nm.SceneManager.OnLoadEventCompleted += OnLoadCompleted;
    }

    void OnDisable()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.SceneManager.OnSceneEvent -= HandleSceneEvent;
        nm.SceneManager.OnLoadEventCompleted -= OnLoadCompleted;
    }

    // เรียกทุกครั้งที่มี SceneEvent ต่อ "client หนึ่งคน"
    void HandleSceneEvent(SceneEvent e)
    {
        // e.ClientId = ไคลเอนต์ที่ event นี้เกี่ยวข้อง (-1 อาจหมายถึง server หรือไม่ระบุแล้วแต่เวอร์ชัน)
        Debug.Log($"[SceneSync] EVENT={e.SceneEventType} • Scene='{e.SceneName}' • FromClient={e.ClientId} • IsServer={NetworkManager.Singleton.IsServer}");

        // ถ้าอยากจับจังหวะ “โหลดของ client เสร็จ” ใช้ LoadComplete
        if (e.SceneEventType == SceneEventType.LoadComplete)
        {
            Debug.Log($"[SceneSync] LOAD COMPLETE • Scene='{e.SceneName}' • client {e.ClientId}");
        }

        // ถ้ามี UnloadComplete / SynchronizeComplete ก็จะมาในนี้เหมือนกัน
    }

    // สรุปครั้งเดียวหลังการโหลดฉากรอบนี้เสร็จ (ทุกคนที่เกี่ยวข้อง)
    void OnLoadCompleted(string sceneName, LoadSceneMode mode, List<ulong> ok, List<ulong> timeout)
    {
        Debug.Log($"[SceneSync] DONE '{sceneName}' • OK={ok.Count} • TIMEOUT={timeout.Count}");
        foreach (var c in ok) Debug.Log($"  - OK client: {c}");
        foreach (var c in timeout) Debug.Log($"  - TIMEOUT client: {c}");
    }
}
