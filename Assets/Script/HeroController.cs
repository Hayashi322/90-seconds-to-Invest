using System;
using System.Collections;
using System.Collections.Generic;
using Astar;
using Graph;
using UnityEngine;
public class HeroController : MonoBehaviour
{
    [SerializeField] private WaypointController waypointController;
    [SerializeField] private GameObject currentNode;
    [SerializeField] private GameObject targetNode;
    [Space(20)]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float reachOffset = 0.05f; // ปรับให้หยุดได้แม่นยำขึ้น
    private List<GameObject> pathWay = null;
    private int _currentPathIndex = 0;
    void Start()
    {
        StartTravel();
    }
    void FixedUpdate() // ใช้ FixedUpdate เพื่อให้เดินลื่นขึ้น
    {
        PathTraversal();
    }
    private void OnDrawGizmos()
    {
        if (pathWay == null || pathWay.Count < 2)
            return;
        Gizmos.color = Color.red;
        for (var i = 0; i < pathWay.Count - 1; i++)
        {
            Vector2 start = pathWay[i].transform.position;
            Vector2 end = pathWay[i + 1].transform.position;
            DrawArrowGizmo(start, end - start);
        }
    }
    private void DrawArrowGizmo(Vector2 pos, Vector2 direction, float arrowHeadLength = 0.2f, float arrowHeadAngle = 30.0f)
    {
        Vector2 endPos = pos + direction;
        Gizmos.DrawLine(pos, endPos);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 right = Quaternion.Euler(0, 0, arrowHeadAngle) * (-direction.normalized) * arrowHeadLength;
        Vector2 left = Quaternion.Euler(0, 0, -arrowHeadAngle) * (-direction.normalized) * arrowHeadLength;
        Gizmos.DrawLine(endPos, endPos + right);
        Gizmos.DrawLine(endPos, endPos + left);
    }
    public void SetStartNode(int nodeIdx)
    {
        List<GraphNode> nodes = waypointController.GetWaypointGraph().GetAllNodes();
        if (nodeIdx >= 0 && nodeIdx < nodes.Count && nodes[nodeIdx] != null)
        {
            currentNode = nodes[nodeIdx].GameObjectNode;
        }
        Debug.Log("SetStartNode: " + nodeIdx);
    }
    public void SetTargetNode(int nodeIdx)
    {
        List<GraphNode> nodes = waypointController.GetWaypointGraph().GetAllNodes();
        if (nodeIdx >= 0 && nodeIdx < nodes.Count && nodes[nodeIdx] != null)
        {
            targetNode = nodes[nodeIdx].GameObjectNode;
        }
        Debug.Log("SetTargetNode: " + nodeIdx);
    }
    public void StartTravel()
    {
        _currentPathIndex = 0;
        transform.position = currentNode.transform.position;
        if (currentNode == targetNode)
        {
            Debug.LogError("Target is the same as Start!");
            return;
        }
        // ค้นหาเส้นทางโดยใช้ A*
        pathWay = AStar.FindPath(waypointController.GetWaypointGraph(), currentNode, targetNode);
        if (pathWay != null)
        {
            Debug.Log("Path found:");
            foreach (var gameObject in pathWay)
            {
                Debug.Log(gameObject.name);
            }
            waypointController.EnablePathGizmos(false);
        }
        else
        {
            Debug.LogError("AStar can not find path!");
        }
    }
    public void ClearPathway()
    {
        pathWay = null;
        waypointController.EnablePathGizmos(true);
    }
    private void PathTraversal()
    {
        if (pathWay == null || pathWay.Count == 0) return;
        GameObject nextNode = pathWay[_currentPathIndex];
        Vector2 nextPosition = nextNode.transform.position;
        // เคลื่อนที่ไปยัง node ถัดไป
        transform.position = Vector2.MoveTowards(transform.position, nextPosition, moveSpeed * Time.deltaTime);
        // เช็คว่าเดินถึง node แล้วหรือไม่
        if (Vector2.Distance(transform.position, nextPosition) < reachOffset)
        {
            _currentPathIndex++;
            if (_currentPathIndex >= pathWay.Count)
            {
                pathWay = null;
                Debug.Log("Arrived at target node!");
            }
        }
    }
}