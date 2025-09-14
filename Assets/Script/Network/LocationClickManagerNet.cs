using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;
using System.Collections.Generic;

public class LocationClickManagerNet : MonoBehaviour
{
    [SerializeField] private LayerMask clickableMask = ~0;

    [System.Serializable] public class TagToId { public string tag; public int id; }
    [SerializeField] private List<TagToId> mappings = new();

    private Camera cam;
    private HeroControllerNet hero;

    private void Awake() { cam = Camera.main; }

    private void OnEnable()
    {
        TryBindLocalHero();
        HeroControllerNet.LocalPlayerSpawned += OnLocalSpawned;
        HeroControllerNet.LocalPlayerDespawned += OnLocalDespawned;
    }
    private void OnDisable()
    {
        HeroControllerNet.LocalPlayerSpawned -= OnLocalSpawned;
        HeroControllerNet.LocalPlayerDespawned -= OnLocalDespawned;
    }

    private void TryBindLocalHero()
    {
        if (hero) return;
        var obj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (obj) hero = obj.GetComponent<HeroControllerNet>();
        Debug.Log($"[ClickMgr] bind hero = {hero}");
    }
    private void OnLocalSpawned(HeroControllerNet h) { hero = h; Debug.Log("[ClickMgr] Local player spawned."); }
    private void OnLocalDespawned() { hero = null; }

    private void Update()
    {
        if (!hero || !hero.IsOwner) return;
        if (Input.GetMouseButtonDown(0)) DetectClick();
    }

    private void DetectClick()
    {
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) { Debug.Log("[ClickMgr] blocked by UI"); return; }

        Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
        var col = Physics2D.OverlapPoint(p, clickableMask);
        Debug.Log($"[ClickMgr] world click at {p} hit={(col ? col.name : "null")}");
        if (!col) return;

        int id = -1;
        var wid = col.GetComponent<WaypointId>();
        if (wid) id = wid.Id;
        else
        {
            var proxy = col.GetComponent<WaypointClickProxy>()
                     ?? col.GetComponentInParent<WaypointClickProxy>();
            if (proxy) id = proxy.GetId();
        }
        if (id >= 0) hero.RequestMoveToWaypoint(id);



        Debug.Log($"[ClickMgr] resolved waypoint id = {id}");
        if (id >= 0) hero.RequestMoveToWaypoint(id);
    }
}
