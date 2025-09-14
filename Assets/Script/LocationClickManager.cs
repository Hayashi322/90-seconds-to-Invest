using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LocationClickManager : MonoBehaviour
{
    /*[SerializeField] private HeroController heroController;

    [Header("Tag → Waypoint")]
    [SerializeField] private List<TagToWaypoint> mappings = new List<TagToWaypoint>();

    [Header("Click Settings")]
    [SerializeField] private LayerMask clickableMask = ~0; // เลเยอร์ที่อนุญาตให้คลิกได้
    private Camera cam;

    [System.Serializable]
    public class TagToWaypoint
    {
        public string tag;
        public GameObject waypoint;
    }

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            DetectLocationClick();
    }

    private void DetectLocationClick()
    {
        // ถ้าคลิกบน UI ไม่ต้องทำอะไร
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        // ดีกว่า Raycast ทิศทางศูนย์: ใช้ OverlapPoint
        Collider2D col = Physics2D.OverlapPoint(mouseWorld, clickableMask);
        if (!col) return;

        MoveToLocation(col.gameObject);
    }

    private void MoveToLocation(GameObject clicked)
    {
        if (!heroController) return;

        // หา waypoint จาก tag
        var wp = FindWaypointByTag(clicked.tag);
        if (wp == null)
        {
            Debug.Log("ไม่มี mapping สำหรับ tag: " + clicked.tag);
            return;
        }

        heroController.SetDestinationByClick(wp);
    }

    private GameObject FindWaypointByTag(string tag)
    {
        for (int i = 0; i < mappings.Count; i++)
        {
            if (mappings[i].tag == tag) return mappings[i].waypoint;
        }
        return null;
    }*/
}
