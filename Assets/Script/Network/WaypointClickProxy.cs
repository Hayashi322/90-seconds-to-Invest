using UnityEngine;

public class WaypointClickProxy : MonoBehaviour
{
    public GameObject waypointNode;   

    public int GetId()
    {
        if (!waypointNode) return -1;
        var wid = waypointNode.GetComponent<WaypointId>();
        if (wid) return wid.Id;
        if (int.TryParse(waypointNode.name, out var n)) return n; 
        return -1;
    }
}
