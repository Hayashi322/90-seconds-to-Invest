using UnityEngine;

[RequireComponent(typeof(Collider2D))]   // ✅ กันลืม ต้องมี Collider2D ถึงจะโดน hover
public class WaypointClickProxy : MonoBehaviour
{
    [Header("Waypoint Node")]
    public GameObject waypointNode;

    [Header("Hover Scale Settings")]
    public float hoverScaleMultiplier = 1.1f;  // ขยายเท่าไหร่เวลาเอาเมาส์ไปชี้
    public float scaleSpeed = 10f;             // ความเร็วในการเปลี่ยนขนาด

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        // จำสเกลเดิมของตึก
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        // ทำให้การเปลี่ยนสเกลลื่น ๆ ไม่กระตุก
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    private void OnMouseEnter()
    {
        // เมาส์ชี้ → ขยาย
        targetScale = originalScale * hoverScaleMultiplier;
        // Debug.Log($"Hover enter: {gameObject.name}");
    }

    private void OnMouseExit()
    {
        // เมาส์ออก → กลับขนาดเดิม
        targetScale = originalScale;
        // Debug.Log($"Hover exit: {gameObject.name}");
    }

    private void OnDisable()
    {
        // กันสเกลค้างเวลาเปลี่ยนซีน / ปิด object
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    public int GetId()
    {
        if (!waypointNode) return -1;

        var wid = waypointNode.GetComponent<WaypointId>();
        if (wid) return wid.Id;

        if (int.TryParse(waypointNode.name, out var n)) return n;

        return -1;
    }
}
