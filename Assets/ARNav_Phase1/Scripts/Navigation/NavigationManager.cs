using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(10)]
public class NavigationManager : MonoBehaviour
{
    public static NavigationManager I;
    public GraphDatabase graph;
    public string CurrentNodeId;
    public string DestinationNodeId = "L1_R105"; // Default destination
    public List<Vector3> CurrentWorldPath = new();
    public bool IsNavigating { get; private set; }

    [Header("Database")]
    public bool loadFromDatabase = true;

    [Header("Tracking Parameters")]
    public float waypointThreshold = 1.2f;    // Distance to pop reached waypoint
    public float offTrackThreshold = 6.0f;     // Distance off-route before auto-recalculating

    private float lastTrackingUpdateTime = 0f;

    void Awake() 
    { 
        I = this; 
        if (graph == null)
        {
            graph = Resources.Load<GraphDatabase>("DuplexGraph");
        }
    }

    IEnumerator Start()
    {
        // Wait one frame so DatabaseManager (order -100) finishes initialization.
        yield return null;
        if (loadFromDatabase && graph != null)
        {
            // Preserve the authored asset graph; only populate from DB if asset is empty
            if (graph.nodes.Count == 0 || graph.edges.Count == 0)
            {
                graph.LoadFromDatabase();
            }
        }
    }

    void OnEnable()
    {
        if (LocalizationManager.I != null)
            LocalizationManager.I.OnAnchorChanged += SyncLocalization;
    }

    void OnDisable()
    {
        if (LocalizationManager.I != null)
            LocalizationManager.I.OnAnchorChanged -= SyncLocalization;
    }

    void Update()
    {
        if (!IsNavigating || CurrentWorldPath == null || CurrentWorldPath.Count == 0 || Camera.main == null)
            return;

        if (Time.time - lastTrackingUpdateTime > 0.1f)
        {
            lastTrackingUpdateTime = Time.time;
            UpdateLiveTracking();
        }
    }

    void UpdateLiveTracking()
    {
        Vector3 camPos = Camera.main.transform.position;

        // 1. Check if user reached the next waypoint
        if (CurrentWorldPath.Count > 1)
        {
            Vector3 nextWaypoint = CurrentWorldPath[1];
            float distToNext = Vector2.Distance(
                new Vector2(camPos.x, camPos.z),
                new Vector2(nextWaypoint.x, nextWaypoint.z)
            );

            if (distToNext < waypointThreshold)
            {
                // Waypoint reached! Advance path
                CurrentWorldPath.RemoveAt(1);
            }
        }

        // 2. Keep the first vertex anchored to user's real-time position
        if (CurrentWorldPath.Count > 0)
        {
            float floorY = CurrentWorldPath.Count > 1 ? CurrentWorldPath[1].y : CurrentWorldPath[0].y;
            CurrentWorldPath[0] = new Vector3(camPos.x, floorY, camPos.z);

            // Check if user is wildly off-track (e.g. took a wrong corridor)
            if (CurrentWorldPath.Count > 1)
            {
                float distFromSegment = Vector2.Distance(
                    new Vector2(camPos.x, camPos.z),
                    new Vector2(CurrentWorldPath[1].x, CurrentWorldPath[1].z)
                );
                if (distFromSegment > offTrackThreshold)
                {
                    Debug.Log("[Nav] Off-track detected. Recalculating from new position...");
                    Recalculate();
                    return;
                }
            }

            if (PathVisualManager.I != null)
            {
                PathVisualManager.I.Render(CurrentWorldPath);
            }
        }
    }

    void SyncLocalization(string anchorName)
    {
        CurrentNodeId = NearestNodeToCamera(anchorName);
        Debug.Log("[Nav] Localized at anchor " + anchorName + ", nearest node: " + CurrentNodeId);

        // Recalculate route if active navigation is underway
        if (IsNavigating && !string.IsNullOrEmpty(DestinationNodeId))
        {
            Recalculate();
        }
    }

    public void Recalculate()
    {
        if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
        {
            Debug.LogError("[Nav] Graph is empty or not loaded.");
            return;
        }

        // Always determine the start node based on current real-time camera position!
        string anchor = LocalizationManager.I != null ? LocalizationManager.I.ActiveAnchorName : "";
        CurrentNodeId = NearestNodeToCamera(anchor);
        string startNode = CurrentNodeId;

        if (string.IsNullOrEmpty(startNode))
        {
            startNode = "L1_R101";
        }

        if (string.IsNullOrEmpty(DestinationNodeId))
        {
            DestinationNodeId = "L1_R105";
        }

        startNode = startNode.Replace("Node_", "");
        DestinationNodeId = DestinationNodeId.Replace("Node_", "");

        Debug.Log($"[Nav] Calculating route: {startNode} -> {DestinationNodeId}");
        var ids = DijkstraEngine.FindPath(graph, startNode, DestinationNodeId, true, true);
        if (ids == null || ids.Count == 0)
        {
            Debug.LogWarning("[Nav] No direct route from " + startNode + " to " + DestinationNodeId);
            return;
        }

        var points = DijkstraEngine.ToWorldPath(graph, ids);

        // Prepend user's actual camera position (projected to floor level)
        if (Camera.main != null && points.Count > 0)
        {
            Vector3 camPos = Camera.main.transform.position;
            Vector3 userStartPoint = new Vector3(camPos.x, points[0].y, camPos.z);
            if (Vector2.Distance(new Vector2(camPos.x, camPos.z), new Vector2(points[0].x, points[0].z)) > 0.5f)
            {
                points.Insert(0, userStartPoint);
            }
        }

        CurrentWorldPath = points;
        IsNavigating = true;

        if (PathVisualManager.I != null)
        {
            PathVisualManager.I.Render(CurrentWorldPath);
        }
    }

    public void StopNavigation()
    {
        IsNavigating = false;
        CurrentWorldPath.Clear();
        if (PathVisualManager.I != null)
        {
            PathVisualManager.I.Render(null);
        }
    }

    public string NearestNodeToCamera(string anchor = null)
    {
        if (Camera.main == null || graph == null || graph.nodes == null || graph.nodes.Count == 0)
            return null;

        Vector3 cameraPos = Camera.main.transform.position;

        // Determine current floor from anchor or camera height
        int targetFloor = 1;
        if (!string.IsNullOrEmpty(anchor))
        {
            if (anchor.Contains("Top_Floor") || anchor.Contains("AT_03") || anchor.Contains("AT_04"))
                targetFloor = 2;
            else if (anchor.Contains("Ground") || anchor.Contains("AT_01"))
                targetFloor = 1;
            else if (anchor.Contains("Stair") || anchor.Contains("AT_02"))
                targetFloor = cameraPos.y > 0.8f ? 2 : 1;
        }
        else
        {
            targetFloor = cameraPos.y > 0.8f ? 2 : 1;
        }

        // Clean anchor ID to match AuthoritativeArea (e.g. "AT_01_Ground_Floor" -> "AT_01")
        string cleanArea = "";
        if (!string.IsNullOrEmpty(anchor))
        {
            if (anchor.Contains("AT_01")) cleanArea = "AT_01";
            else if (anchor.Contains("AT_02")) cleanArea = "AT_02";
            else if (anchor.Contains("AT_03")) cleanArea = "AT_03";
            else if (anchor.Contains("AT_04")) cleanArea = "AT_04";
        }

        float bestDist = float.MaxValue;
        string bestId = null;

        // Pass 1: Match both floor and area
        if (!string.IsNullOrEmpty(cleanArea))
        {
            foreach (var n in graph.nodes)
            {
                int nodeFloor = n.Id.StartsWith("L2_") ? 2 : (n.Id.StartsWith("L1_") ? 1 : n.Floor);
                if (n.AuthoritativeArea == cleanArea && nodeFloor == targetFloor)
                {
                    float d = Vector3.Distance(cameraPos, n.Position);
                    if (d < bestDist) { bestDist = d; bestId = n.Id; }
                }
            }
        }

        // Pass 2: Match floor only if no area match found
        if (bestId == null)
        {
            foreach (var n in graph.nodes)
            {
                int nodeFloor = n.Id.StartsWith("L2_") ? 2 : (n.Id.StartsWith("L1_") ? 1 : n.Floor);
                if (nodeFloor == targetFloor)
                {
                    float d = Vector3.Distance(cameraPos, n.Position);
                    if (d < bestDist) { bestDist = d; bestId = n.Id; }
                }
            }
        }

        // Pass 3: Global fallback
        if (bestId == null)
        {
            foreach (var n in graph.nodes)
            {
                float d = Vector3.Distance(cameraPos, n.Position);
                if (d < bestDist) { bestDist = d; bestId = n.Id; }
            }
        }

        return bestId != null ? bestId.Replace("Node_", "") : null;
    }
}
