using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class ARNavEditorMenu
{
    // ─── Graph helpers ───
    static GraphDatabase LoadGraph()
    {
        var nav = UnityEngine.Object.FindAnyObjectByType<NavigationManager>();
        if (nav?.graph != null) return nav.graph;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GraphDatabase>("Assets/ARNav_Phase1/Data/DuplexGraph.asset");
    }

    static LineRenderer GetLineRenderer()
    {
        var go = GameObject.Find("PathRenderer");
        var lr = go?.GetComponent<LineRenderer>();
        if (lr != null) lr.useWorldSpace = true;
        return lr;
    }

    // ─── Catmull-Rom smoothing (copied from PathVisualManager) ───
    static List<Vector3> SmoothPath(List<Vector3> pts, int samples = 8)
    {
        if (pts.Count < 3) return new List<Vector3>(pts);
        var outPts = new List<Vector3>();
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = (i + 2 < pts.Count) ? pts[i + 2] : pts[i + 1];
            for (int j = 0; j < samples; j++)
            {
                float t = j / (float)samples;
                outPts.Add(0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t));
            }
        }
        outPts.Add(pts[pts.Count - 1]);
        return outPts;
    }

    static void RenderPath(List<Vector3> worldPath, float heightOffset = 0.35f)
    {
        var lr = GetLineRenderer();
        if (lr == null) { Debug.LogWarning("[ARNav] No PathRenderer LineRenderer found"); return; }
        var smooth = SmoothPath(worldPath);
        lr.positionCount = smooth.Count;
        for (int i = 0; i < smooth.Count; i++)
            lr.SetPosition(i, smooth[i] + Vector3.up * heightOffset);
        Debug.Log($"[ARNav] Rendered path: {worldPath.Count} nodes → {smooth.Count} points");
    }

    // ─── Menu Items ───
    [MenuItem("Tools/ARNav/Test Path: L1_R109 → L1_R100 (Full Corridor)")]
    static void TestPath_FullCorridor() => TestPath("L1_R109", "L1_R100");

    [MenuItem("Tools/ARNav/Test Path: L1_CorD → L1_R101 (Short)")]
    static void TestPath_Short() => TestPath("L1_CorD", "L1_R101");

    [MenuItem("Tools/ARNav/Test Path: L1_CorA → L1_R108 (Direct)")]
    static void TestPath_Direct() => TestPath("L1_CorA", "L1_R108");

    [MenuItem("Tools/ARNav/Test Path: L1_AT01_EXIT → L2_AT03_ENTRY (Cross-Floor)")]
    static void TestPath_CrossFloor() => TestPath("L1_AT01_EXIT", "L2_AT03_ENTRY");

    [MenuItem("Tools/ARNav/Test Path: Custom...")]
    static void TestPath_Custom()
    {
        var graph = LoadGraph();
        if (graph == null) { Debug.LogWarning("[ARNav] No graph"); return; }
        var nodeNames = graph.nodes.Select(n => n.Id).OrderBy(n => n).ToArray();
        var start = EditorUtility.DisplayDialogComplex("ARNav Path Test", "Select start node:", "Cancel", "OK", "") == 1 ? "" : "";
        // For custom, just use a simple prompt - we'll add a proper window later
        TestPath("L1_R109", "L1_R100");
    }

    static void TestPath(string startId, string destId)
    {
        var graph = LoadGraph();
        if (graph == null) { Debug.LogWarning("[ARNav] No graph loaded"); return; }

        var path = DijkstraEngine.FindPath(graph, startId, destId, true, true);
        if (path == null) { Debug.Log($"[ARNav] No path {startId} → {destId}"); return; }

        var worldPath = DijkstraEngine.ToWorldPath(graph, path);
        RenderPath(worldPath);
        Debug.Log($"[ARNav] Path {startId} → {destId}: {string.Join(" → ", path)} ({worldPath.Count} pts)");
    }

    [MenuItem("Tools/ARNav/Re-bake Graph from Scene")]
    static void BakeGraph()
    {
        var nav = UnityEngine.Object.FindAnyObjectByType<NavigationManager>();
        if (nav != null)
        {
            var method = nav.GetType().GetMethod("BakeFromScene", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) method.Invoke(nav, null);
            Debug.Log("[ARNav] Graph re-baked from scene");
        }
        else Debug.LogWarning("[ARNav] No NavigationManager in scene");
    }

    [MenuItem("Tools/ARNav/Clear Path Visualization")]
    static void ClearPath()
    {
        var lr = GetLineRenderer();
        if (lr != null) lr.positionCount = 0;
        Debug.Log("[ARNav] Path cleared");
    }

    [MenuItem("Tools/ARNav/Log Graph Info")]
    static void LogGraphInfo()
    {
        var graph = LoadGraph();
        if (graph == null) { Debug.LogWarning("[ARNav] No graph"); return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Nodes: {graph.nodes.Count}");
        foreach (var n in graph.nodes.OrderBy(n => n.AuthoritativeArea).ThenBy(n => n.Id))
            sb.AppendLine($"  {n.Id} pos={n.Position:F2} floor={n.Floor} area={n.AuthoritativeArea} trans={n.IsTransition}");
        sb.AppendLine($"Edges: {graph.edges.Count}");
        foreach (var e in graph.edges.Where(e => e.Enabled))
            sb.AppendLine($"  {e.From} → {e.To} cost={e.Cost} stairs={e.IsStairs} elev={e.IsElevator}");
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/ARNav/Find Missing Stair Edges")]
    static void FindMissingStairEdges()
    {
        var graph = LoadGraph();
        if (graph == null) return;
        
        var stairNodes = graph.nodes.Where(n => n.IsTransition).Select(n => n.Id).ToList();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Transition nodes (no edges connected):");
        foreach (var id in stairNodes)
        {
            var fromEdges = graph.edges.Where(e => e.From == id && e.Enabled).ToList();
            var toEdges = graph.edges.Where(e => e.To == id && e.Enabled).ToList();
            if (fromEdges.Count == 0 && toEdges.Count == 0)
                sb.AppendLine($"  {id} — NO EDGES");
            else
            {
                sb.AppendLine($"  {id} — out:{fromEdges.Count} in:{toEdges.Count}");
                foreach (var e in fromEdges) sb.AppendLine($"    → {e.To}");
                foreach (var e in toEdges) sb.AppendLine($"    ← {e.From}");
            }
        }
        Debug.Log(sb.ToString());
    }
}