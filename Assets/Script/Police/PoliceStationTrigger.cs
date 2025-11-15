using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PoliceStationTrigger : MonoBehaviour
{
    [SerializeField] private PoliceStationUI ui;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var law = other.GetComponentInParent<PlayerLawState>();
        if (law == null)
        {
            Debug.Log("[Police] No PlayerLawState on object that entered station.");
            return;
        }

        if (!law.IsOwner)
        {
            Debug.Log("[Police] Player entered is not local owner, ignore.");
            return;
        }

        if (ui == null)
        {
            Debug.LogWarning("[Police] PoliceStationUI is not assigned.");
            return;
        }

        Debug.Log($"[Police] Open station UI for client {law.OwnerClientId}");
        //ui.OpenForLocalPlayer(law);
    }
}
