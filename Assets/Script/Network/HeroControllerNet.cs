using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Astar;
using Graph;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class HeroControllerNet : NetworkBehaviour
{
    public static event Action<HeroControllerNet> LocalPlayerSpawned;
    public static event Action LocalPlayerDespawned;

    [Header("UI (Owner only)")]
    [SerializeField] private GameObject MapCanvas;

    [Header("Pathfinding")]
    [SerializeField] private WaypointController waypointController;
    [SerializeField] private GameObject startWaypoint; // optional
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float reachOffset = 0.05f;

    [Header("Character Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Sprite[] characterSpritesInGame;

    public NetworkVariable<int> CharacterIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ตอนนี้ไม่ได้ใช้บล็อกเดินแล้ว แต่เก็บไว้เผื่อ debug
    public NetworkVariable<bool> PauseByUI = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private List<GameObject> pathWay;
    private int _currentPathIndex;
    private GameObject currentNode;
    private GameObject targetNode;

    private SpriteRenderer _sr;

    // ใช้เช็คคุก
    private PlayerLawState lawState;

    private void Awake()
    {
        if (!MapCanvas)
            MapCanvas = GameObject.FindGameObjectWithTag("MapCanvas")
                     ?? GameObject.Find("Map")
                     ?? GameObject.Find("MapCanvas");

        if (!waypointController)
#if UNITY_2023_1_OR_NEWER
            waypointController = FindFirstObjectByType<WaypointController>();
#else
            waypointController = FindObjectOfType<WaypointController>();
#endif

        _sr = GetComponentInChildren<SpriteRenderer>();
        if (!bodyRenderer) bodyRenderer = _sr;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        UpdateVisible();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateVisible();
    }

    private void UpdateVisible()
    {
        if (_sr == null) return;

        bool isGameScene = SceneManager.GetActiveScene().name == "GameSceneNet";
        _sr.enabled = isGameScene;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        lawState = GetComponent<PlayerLawState>();

        if (IsClient)
        {
            CharacterIndex.OnValueChanged += OnCharacterIndexChanged;
            OnCharacterIndexChanged(-1, CharacterIndex.Value);
        }

        if (IsOwner) LocalPlayerSpawned?.Invoke(this);
        if (IsServer) StartCoroutine(ServerInitAfterSpawn());
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
            CharacterIndex.OnValueChanged -= OnCharacterIndexChanged;

        if (IsOwner) LocalPlayerDespawned?.Invoke();
        base.OnNetworkDespawn();
    }

    private void OnCharacterIndexChanged(int prev, int current)
    {
        ApplyCharacterVisual(current);
    }

    private void ApplyCharacterVisual(int index)
    {
        if (!bodyRenderer) return;
        if (index < 0 || index >= characterSpritesInGame.Length) return;
        bodyRenderer.sprite = characterSpritesInGame[index];
    }

    public void ServerSetCharacterIndex(int index)
    {
        if (!IsServer) return;
        CharacterIndex.Value = index;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // ยังให้คุกหยุดเดินปกติ
        if (lawState != null && lawState.IsInJail.Value)
            return;

        // ไม่ใช้ PauseByUI บล็อกเดินแล้ว
        // if (PauseByUI.Value) return;

        PathTraversalServer();
    }

    public void ServerResetPathAfterTeleport()
    {
        if (!IsServer) return;

        pathWay = null;
        _currentPathIndex = 0;
        currentNode = null;
        targetNode = null;

        Debug.Log("[HeroControllerNet] Reset path & current node after teleport.");
    }

    public void SetUIOpen(bool isOpen)
    {
        if (!IsOwner) return;
        SetUIOpenServerRpc(isOpen);
    }

    [ServerRpc]
    private void SetUIOpenServerRpc(bool isOpen)
    {
        PauseByUI.Value = isOpen;
    }

    public void RequestMoveToWaypoint(int waypointId)
    {
        if (!IsOwner) return;
        SetDestinationServerRpc(waypointId);
    }

    [ServerRpc]
    private void SetDestinationServerRpc(int waypointId)
    {
        if (!IsServer) return;
        if (!EnsureWaypointsReady())
        {
            Debug.LogError("[HeroControllerNet] waypointController/graph not ready.");
            return;
        }

        var graph = waypointController.GetWaypointGraph();
        var nodes = graph.GetAllNodes();

        var startGo = currentNode ?? waypointController.GetClosestGraphNode(transform.position);
        var goalGo = waypointController.GetGraphNodeById(waypointId);

        bool InGraph(GameObject go) => go && nodes.Exists(n => n.GameObjectNode == go);

        Debug.Log($"[HeroControllerNet] Move request: start={startGo?.name ?? "null"} goal={goalGo?.name ?? "null"} (id={waypointId})");

        if (!InGraph(startGo) || !InGraph(goalGo))
        {
            Debug.LogError("[HeroControllerNet] Start or Goal node not found IN THE GRAPH.");
            return;
        }

        currentNode = startGo;
        targetNode = goalGo;

        // ⭐ กรณีคลิกที่ node เดิม (เช่น ยืนอยู่ที่ policeWaypoint แล้วคลิกซ้ำ)
        if (currentNode == targetNode)
        {
            int canvasNumber = ComputeCanvasNumberFromNode(currentNode);
            if (canvasNumber >= 0)
            {
                var sendParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
                };
                OpenCanvasClientRpc(canvasNumber, sendParams);
                Debug.Log($"[HeroControllerNet] Immediate open canvas {canvasNumber} at node {currentNode.name} (start==goal).");
            }

            // ไม่ต้องเดิน ใช้แค่นี้พอ
            pathWay = null;
            _currentPathIndex = 0;
            return;
        }

        // ปกติ: start != goal → ใช้ A* เดินไป
        pathWay = AStar.FindPath(graph, currentNode, targetNode);
        if (pathWay != null && pathWay.Count > 0)
        {
            _currentPathIndex = 0;
            Debug.Log($"[HeroControllerNet] Path len = {pathWay.Count}");
        }
        else
        {
            Debug.LogError("[HeroControllerNet] A* returned null/empty (no connected edges?).");
            pathWay = null;
        }
    }

    private void PathTraversalServer()
    {
        if (pathWay == null || pathWay.Count == 0) return;
        if (_currentPathIndex < 0 || _currentPathIndex >= pathWay.Count)
        {
            Debug.LogError($"[HeroControllerNet] Path index OOR: {_currentPathIndex}");
            pathWay = null;
            return;
        }

        var nextNode = pathWay[_currentPathIndex];
        var nextPos = (Vector2)nextNode.transform.position;

        transform.position = Vector2.MoveTowards(transform.position, nextPos, moveSpeed * Time.deltaTime);

        if (Vector2.Distance((Vector2)transform.position, nextPos) < reachOffset)
        {
            _currentPathIndex++;
            if (_currentPathIndex >= pathWay.Count)
            {
                pathWay = null;
                currentNode = targetNode;
                targetNode = null;

                int canvasNumber = ComputeCanvasNumberFromNode(currentNode);

                var sendParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
                };
                OpenCanvasClientRpc(canvasNumber, sendParams);

                Debug.Log($"[HeroControllerNet] Arrived: {currentNode.name} (canvas {canvasNumber}).");
            }
        }
    }

    private IEnumerator ServerInitAfterSpawn()
    {
        while (SceneManager.GetActiveScene().name != "GameSceneNet")
        {
            yield return null;
        }

        float timeout = 8f, t = 0f;
        while (!EnsureWaypointsReady())
        {
            t += Time.deltaTime;
            if (t > timeout)
            {
                Debug.LogError($"[HeroControllerNet] Waypoints not ready. graphNodes={GraphNodeCount()}");
                yield break;
            }
            yield return null;
        }

        GameObject startFromGraph = null;
        if (startWaypoint)
        {
            int id = -1;
            var wid = startWaypoint.GetComponent<WaypointId>();
            if (wid) id = wid.Id;
            else if (int.TryParse(startWaypoint.name, out var n)) id = n;
            if (id >= 0) startFromGraph = waypointController.GetGraphNodeById(id);
        }

        currentNode = startFromGraph ?? waypointController.GetClosestGraphNode(transform.position);

        if (currentNode != null)
        {
            transform.position = currentNode.transform.position;
            Debug.Log($"[HeroControllerNet] Start at {currentNode.name}");
        }
        else
        {
            Debug.LogError("[HeroControllerNet] No start/closest waypoint found IN GRAPH.");
        }
    }

    private bool EnsureWaypointsReady()
    {
        if (!waypointController)
#if UNITY_2023_1_OR_NEWER
            waypointController = FindFirstObjectByType<WaypointController>();
#else
            waypointController = FindObjectOfType<WaypointController>();
#endif
        if (!waypointController) return false;

        var g = waypointController.GetWaypointGraph();
        var list = g?.GetAllNodes();
        return (list != null && list.Count > 0);
    }

    private int GraphNodeCount()
    {
        var g = waypointController?.GetWaypointGraph();
        return g?.GetAllNodes()?.Count ?? 0;
    }

    private static int GetNodeId(GameObject node)
    {
        if (!node) return -1;
        var wid = node.GetComponent<WaypointId>();
        if (wid) return wid.Id;

        var m = Regex.Match(node.name, @"\d+");
        if (m.Success && int.TryParse(m.Value, out var parsed)) return parsed;
        return -1;
    }

    private int ComputeCanvasNumberFromNode(GameObject node)
    {
        int id = GetNodeId(node);
        return id switch
        {
            1 => 0,
            2 => 1,
            5 => 2,
            7 => 3,
            9 => 4,
            12 => 5,
            16 => 6,
            18 => 7,
            _ => -1,
        };
    }

    [ClientRpc]
    private void OpenCanvasClientRpc(int canvasNum, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        if (canvasNum < 0) return;

#if UNITY_2023_1_OR_NEWER
        var opener = FindFirstObjectByType<OpenCanvas>(FindObjectsInactive.Include);
#else
        var opener = FindObjectOfType<OpenCanvas>(true);
#endif
        if (!opener)
        {
            Debug.LogWarning("[HeroControllerNet] OpenCanvas not found in scene.");
            return;
        }
        opener.openCanvas(canvasNum);
    }
}
