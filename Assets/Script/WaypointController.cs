using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Graph;

public class WaypointController : MonoBehaviour
{
    [Serializable] public class NeighborData { public GameObject node; public float weight = 1f; }
    [Serializable] public class WaypointData { public GameObject node; public NeighborData[] neighbors; }

    [SerializeField] private WaypointData[] WaypointList;
    [SerializeField] private bool flagDrawGizmo = true;

    private WeightedGraph waypointGraph;
    private readonly Dictionary<int, GameObject> idLookup = new();

    public bool IsReady
        => waypointGraph != null && waypointGraph.GetAllNodes() != null && waypointGraph.GetAllNodes().Count > 0;

    private void Awake()
    {
        BuildGraphIfNeeded();
        BuildIdLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildGraphIfNeeded(true);
        BuildIdLookup();
    }
#endif

    // ===== Public API =====
    public WeightedGraph GetWaypointGraph()
    {
        BuildGraphIfNeeded();
        return waypointGraph;
    }

    public void EnablePathGizmos(bool enable) => flagDrawGizmo = enable;

    public bool TryGetNodeById(int id, out GameObject go) => idLookup.TryGetValue(id, out go);

    /// <summary>ดึง GameObject ของโหนด "ตัวจริงในกราฟ" ตาม id (ดูทั้ง WaypointId และตัวเลขในชื่อ)</summary>
    public GameObject GetGraphNodeById(int id)
    {
        var nodes = GetWaypointGraph()?.GetAllNodes();
        if (nodes != null)
        {
            foreach (var n in nodes)
            {
                var go = n.GameObjectNode; if (!go) continue;
                var wid = go.GetComponent<WaypointId>();
                if (wid && wid.Id == id) return go;

                if (!wid && TryParseDigits(go.name, out var num) && num == id) return go;
            }
        }

        // fallback จาก idLookup (เผื่อไม่ได้อยู่ในกราฟแต่เป็นลูกใต้คอนโทรลเลอร์)
        if (idLookup.TryGetValue(id, out var found)) return found;
        return null;
    }

    /// <summary>หาโหนดในกราฟที่อยู่ใกล้ตำแหน่ง pos ที่สุด (fallback เป็นลูกใต้คอนโทรลเลอร์ถ้ากราฟว่าง)</summary>
    public GameObject GetClosestGraphNode(Vector2 pos)
    {
        var nodes = GetWaypointGraph()?.GetAllNodes();
        GameObject best = null; float bestD = float.MaxValue;

        if (nodes != null)
        {
            foreach (var n in nodes)
            {
                var go = n.GameObjectNode; if (!go) continue;
                float d = Vector2.Distance(pos, go.transform.position);
                if (d < bestD) { bestD = d; best = go; }
            }
        }

        if (!best) // fallback ลูกใต้คอนโทรลเลอร์
        {
            foreach (Transform c in transform)
            {
                float d = Vector2.Distance(pos, c.position);
                if (d < bestD) { bestD = d; best = c.gameObject; }
            }
        }
        return best;
    }

    // ===== Internal =====
    private void BuildGraphIfNeeded(bool force = false)
    {
        if (!force && waypointGraph != null) return;

        waypointGraph = new WeightedGraph();
        if (WaypointList == null) return;

        // Add nodes
        foreach (var wp in WaypointList)
        {
            if (wp != null && wp.node != null)
                waypointGraph.AddNode(wp.node);
        }

        // Add edges
        foreach (var wp in WaypointList)
        {
            if (wp == null || wp.node == null || wp.neighbors == null) continue;
            foreach (var nb in wp.neighbors)
            {
                if (nb != null && nb.node != null)
                    waypointGraph.AddEdge(wp.node, nb.node, nb.weight);
            }
        }

        waypointGraph.PrintGraph();
    }

    private void BuildIdLookup()
    {
        idLookup.Clear();

        // 1) ลูกใต้ GameObject นี้ (เช่น "01","02",…)
        foreach (Transform child in transform)
        {
            var go = child.gameObject;
            if (TryGetIdFromGO(go, out var id) && !idLookup.ContainsKey(id))
                idLookup[id] = go;
        }

        // 2) รวมจาก WaypointList ด้วย (ถ้ามี)
        if (WaypointList != null)
        {
            foreach (var wp in WaypointList)
            {
                if (wp?.node == null) continue;
                if (TryGetIdFromGO(wp.node, out var id) && !idLookup.ContainsKey(id))
                    idLookup[id] = wp.node;
            }
        }
    }

    private static bool TryGetIdFromGO(GameObject go, out int id)
    {
        id = -1;
        if (!go) return false;

        var wid = go.GetComponent<WaypointId>();
        if (wid) { id = wid.Id; return true; }

        return TryParseDigits(go.name, out id);
    }

    private static bool TryParseDigits(string s, out int num)
    {
        num = -1; // กำหนดค่าเริ่มต้นเสมอ
        var m = Regex.Match(s ?? string.Empty, @"\d+");
        if (!m.Success) return false;
        return int.TryParse(m.Value, out num);
    }


    private void OnDrawGizmos()
    {
        if (!flagDrawGizmo || WaypointList == null) return;

        foreach (var wp in WaypointList)
        {
            if (wp?.node == null) continue;

            Vector2 pos = wp.node.transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos, 0.1f);

            if (wp.neighbors == null) continue;
            Gizmos.color = Color.yellow;
            foreach (var nb in wp.neighbors)
            {
                if (nb?.node == null) continue;
                Vector2 npos = nb.node.transform.position;
                DrawArrowGizmo(pos, npos - pos);
            }
        }
    }

    private void DrawArrowGizmo(Vector2 pos, Vector2 dir, float headLen = 0.2f, float headAngle = 30f)
    {
        Vector2 end = pos + dir;
        Gizmos.DrawLine(pos, end);
        Vector2 right = Quaternion.Euler(0, 0, headAngle) * (-dir.normalized) * headLen;
        Vector2 left = Quaternion.Euler(0, 0, -headAngle) * (-dir.normalized) * headLen;
        Gizmos.DrawLine(end, end + right);
        Gizmos.DrawLine(end, end + left);
    }
}
