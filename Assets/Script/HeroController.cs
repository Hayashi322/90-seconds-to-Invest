using System;
using System.Collections;
using System.Collections.Generic;
using Astar;
using Graph;
using UnityEngine;
public class HeroController : MonoBehaviour
{
    [SerializeField] private WaypointController waypointController;
    [SerializeField] private GameObject startWaypoint; // จุดเริ่มต้นที่กำหนด
    [SerializeField] private GameObject targetNode;
    [Space(20)]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float reachOffset = 0.05f;
    private List<GameObject> pathWay = null;
    private int _currentPathIndex = 0;
    private GameObject currentNode; // ใช้เก็บ Waypoint ปัจจุบัน
    void Start()
    {
        if (startWaypoint != null)
        {
            currentNode = startWaypoint;
            transform.position = currentNode.transform.position; // ให้ตัวละครเริ่มต้นที่ตำแหน่งของ Waypoint
        }
        else
        {
            currentNode = FindClosestWaypoint();
            transform.position = currentNode.transform.position;
        }
    }
    void FixedUpdate()
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
    public void SetDestinationByClick(GameObject targetWaypoint)
    {
        targetNode = targetWaypoint;
        StartTravel();
    }
    private void StartTravel()
    {
        if (currentNode == null || targetNode == null)
        {
            Debug.LogError("Waypoint ไม่ถูกต้อง!");
            return;
        }
        if (currentNode == targetNode)
        {
            Debug.Log("อยู่ที่เป้าหมายแล้ว ไม่ต้องเดิน");
            return;
        }
        // คำนวณเส้นทาง A*
        pathWay = AStar.FindPath(waypointController.GetWaypointGraph(), currentNode, targetNode);
        if (pathWay != null && pathWay.Count > 0)
        {
            Debug.Log("Path found:");
            foreach (var gameObject in pathWay)
            {
                Debug.Log(gameObject.name);
            }
            _currentPathIndex = 0; // รีเซ็ตให้เริ่มเดินจากจุดแรก
            waypointController.EnablePathGizmos(false);
        }
        else
        {
            Debug.LogError("AStar ไม่สามารถหาเส้นทางได้!");
            pathWay = null; // ป้องกันการเดินผิดเส้นทาง
        }
    }
    private GameObject FindClosestWaypoint()
    {
        List<GraphNode> nodes = waypointController.GetWaypointGraph().GetAllNodes();
        GameObject closestNode = null;
        float closestDistance = float.MaxValue;
        foreach (var node in nodes)
        {
            float distance = Vector2.Distance(transform.position, node.GameObjectNode.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node.GameObjectNode;
            }
        }
        return closestNode;
    }
    private void PathTraversal()
    {
        if (pathWay == null || pathWay.Count == 0) return;
        // ตรวจสอบว่าดัชนี `_currentPathIndex` อยู่ในขอบเขตที่ถูกต้อง
        if (_currentPathIndex < 0 || _currentPathIndex >= pathWay.Count)
        {
            Debug.LogError("Path Index เกินขอบเขต! _currentPathIndex: " + _currentPathIndex);
            pathWay = null; // หยุดการเดินถ้าเกิดข้อผิดพลาด
            return;
        }
        GameObject nextNode = pathWay[_currentPathIndex];
        Vector2 nextPosition = nextNode.transform.position;
        // เดินตรงไปยัง Waypoint เท่านั้น (เกาะเส้นทาง)
        transform.position = Vector2.MoveTowards(transform.position, nextPosition, moveSpeed * Time.deltaTime);
        // ถ้าถึงจุดหมาย ให้ไปยัง Waypoint ถัดไป
        if (Vector2.Distance(transform.position, nextPosition) < reachOffset)
        {
            _currentPathIndex++;
            if (_currentPathIndex >= pathWay.Count)
            {
                // ถึงจุดสุดท้ายของเส้นทางแล้ว
                pathWay = null;
                currentNode = targetNode; // อัปเดตจุดเริ่มต้นใหม่
                targetNode = null; // รอให้เลือกจุดหมายใหม่
                Debug.Log("ถึงเป้าหมายแล้ว! จุดใหม่: " + currentNode.name);
            }
        }
    }
}