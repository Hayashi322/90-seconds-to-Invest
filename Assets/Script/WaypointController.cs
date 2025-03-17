using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using Graph;

public class WaypointController : MonoBehaviour
{
    [Serializable]
    public class NeighborData
    {
        [SerializeField] public GameObject node;
        [SerializeField] public float weight = 1.0f;
    }

    [Serializable]
    public class WaypointData
    {
        [SerializeField] public GameObject node;
        [SerializeField] public NeighborData[] neighbors;
    }

    private WeightedGraph waypoint_graph = null;
    [SerializeField] private WaypointData[] WaypointList;

    [SerializeField] private bool flagDrawGizmo = true;

    void Awake()
    {
        // Singleton graph
        if (waypoint_graph == null)
        {
            waypoint_graph = BuildGraph();
        }
    }

    private void OnDrawGizmos()
    {
        if (WaypointList == null) return;

        foreach (var wp in WaypointList)
        {
            if (wp.node != null)
            {
                Vector2 pos = wp.node.transform.position;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(pos, 0.1f); // วาดจุดของ Waypoint

                if (flagDrawGizmo)
                {
                    Gizmos.color = Color.yellow;
                    foreach (var nb in wp.neighbors)
                    {
                        if (nb.node != null)
                        {
                            Vector2 neighborPos = nb.node.transform.position;
                            Vector2 direction = neighborPos - pos;
                            DrawArrowGizmo(pos, direction);
                        }
                    }
                }
            }
        }
    }

    public void EnablePathGizmos(bool flag)
    {
        flagDrawGizmo = flag;
    }

    private void DrawArrowGizmo(Vector2 pos, Vector2 direction, float arrowHeadLength = 0.2f, float arrowHeadAngle = 30.0f)
    {
        Vector2 endPos = pos + direction;
        Gizmos.DrawLine(pos, endPos); // วาดเส้นทาง

        // วาดหัวลูกศร
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 right = Quaternion.Euler(0, 0, arrowHeadAngle) * (-direction.normalized) * arrowHeadLength;
        Vector2 left = Quaternion.Euler(0, 0, -arrowHeadAngle) * (-direction.normalized) * arrowHeadLength;

        Gizmos.DrawLine(endPos, endPos + right);
        Gizmos.DrawLine(endPos, endPos + left);
    }

    public WeightedGraph GetWaypointGraph()
    {
        return waypoint_graph;
    }

    private WeightedGraph BuildGraph()
    {
        // Build graph
        WeightedGraph graph = new WeightedGraph();

        // Add all nodes
        foreach (var wp in WaypointList)
        {
            if (wp.node != null)
            {
                graph.AddNode(wp.node);
            }
        }

        // Assign all edges
        foreach (var wp in WaypointList)
        {
            foreach (var nb in wp.neighbors)
            {
                if (nb.node != null)
                {
                    graph.AddEdge(wp.node, nb.node, nb.weight);
                }
            }
        }

        graph.PrintGraph();
        return graph;
    }
}