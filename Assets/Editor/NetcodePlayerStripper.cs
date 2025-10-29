#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;

public static class NetcodePlayerStripper
{
    static GameObject GetPlayerPrefab()
    {
        var nm = Object.FindObjectOfType<NetworkManager>();
        if (nm == null)
        {
            Debug.LogError("[Stripper] NetworkManager not found in scene. Open a scene that has it, then run again.");
            return null;
        }
#if NGO_2X_OR_NEWER
        var player = nm.NetworkConfig.PlayerPrefab; // adjust if symbol
#else
        var player = nm.NetworkConfig.PlayerPrefab;
#endif
        if (player == null)
        {
            Debug.LogError("[Stripper] NetworkConfig.PlayerPrefab is null.");
            return null;
        }
        return player.gameObject;
    }

    static bool IsSamePrefabAsset(GameObject instance, GameObject prefabAsset)
    {
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
        return src != null && src == prefabAsset;
    }

    static IEnumerable<GameObject> FindScenePlacedPlayers()
    {
        var playerPrefab = GetPlayerPrefab();
        if (playerPrefab == null) yield break;

        var scene = EditorSceneManager.GetActiveScene();
        var allNOs = Object.FindObjectsOfType<NetworkObject>(true);

        foreach (var no in allNOs)
        {
            if (no == null) continue;

            // ต้องเป็นวัตถุที่ "วางอยู่ในฉาก" (ไม่ใช่ DontDestroy หรือที่ spawn ตอนรัน)
            if (no.gameObject.scene != scene) continue;

            // เทียบว่า prefab ต้นทาง == Player Prefab
            if (IsSamePrefabAsset(no.gameObject, playerPrefab))
                yield return no.gameObject;
        }
    }

    [MenuItem("Tools/Netcode/Audit Scene-placed Player(s) in Current Scene")]
    public static void Audit()
    {
        var offenders = FindScenePlacedPlayers().ToList();
        if (offenders.Count == 0)
        {
            Debug.Log($"[Stripper] OK: No scene-placed Player prefab found in '{EditorSceneManager.GetActiveScene().name}'.");
            return;
        }
        Selection.objects = offenders.ToArray();
        Debug.LogWarning($"[Stripper] Found {offenders.Count} scene-placed Player prefab instance(s). They must be removed. Selected them in Hierarchy.");
    }

    [MenuItem("Tools/Netcode/Strip Scene-placed Player(s) from Current Scene")]
    public static void Strip()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var offenders = FindScenePlacedPlayers().ToList();
        int removed = 0;
        foreach (var go in offenders)
        {
            Object.DestroyImmediate(go);
            removed++;
        }
        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Stripper] Removed {removed} scene-placed Player prefab instance(s) from '{scene.name}'. Save the scene.");
        }
        else
        {
            Debug.Log($"[Stripper] Nothing to remove in '{scene.name}'.");
        }
    }
}
#endif
