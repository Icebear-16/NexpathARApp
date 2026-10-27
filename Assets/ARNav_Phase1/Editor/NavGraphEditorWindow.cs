using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class NavGraphEditorWindow : EditorWindow
{
    private GraphDatabase graph;
    private GameObject nodesRoot;

    private bool showSceneVisuals = true;
    private bool showNodeLabels = true;
    private bool showEdgeLines = true;
    private float handleSize = 0.35f;

    // Route tester
    private int startNodeIdx = 0;
    private int endNodeIdx = 0;
    private List<string> previewPath = new();
    private float previewDistance = 0f;

    // Scroll positions
    private Vector2 mainScroll;
    private Vector2 disconnectedScroll;

    [MenuItem("ARNav/Navigation Graph Editor", false, 10)]
    public static void ShowWindow()
    {
        var window = GetWindow<NavGraphEditorWindow>("Nav Graph Editor");
        window.minSize = new Vector2(380, 520);
        window.Show();
    }

    private void OnEnable()
    {
        FindDefaultReferences();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void FindDefaultReferences()
    {
        if (graph == null)
        {
            graph = AssetDatabase.LoadAssetAtPath<GraphDatabase>("Assets/ARNav_Phase1/Data/DuplexGraph.asset");
        }
        if (nodesRoot == null)
        {
            nodesRoot = GameObject.Find("NodesRoot");
        }
    }

    private void OnGUI()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("AR Navigation Graph Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Visually inspect, connect, and validate building waypoints and corridors.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        // Target References
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        graph = (GraphDatabase)EditorGUILayout.ObjectField("Graph Database", graph, typeof(GraphDatabase), false);
        nodesRoot = (GameObject)EditorGUILayout.ObjectField("Scene Nodes Root", nodesRoot, typeof(GameObject), true);
        EditorGUILayout.EndVertical();

        if (graph == null)
        {
            EditorGUILayout.HelpBox("Please assign a GraphDatabase asset (e.g. DuplexGraph.asset).", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // Section: Scene Visualization Toggles
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Scene View Visualization", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        showSceneVisuals = EditorGUILayout.Toggle("Show Visuals in Scene", showSceneVisuals);
        if (showSceneVisuals)
        {
            showNodeLabels = EditorGUILayout.Toggle("Show Node Labels", showNodeLabels);
            showEdgeLines = EditorGUILayout.Toggle("Show Edge Connections", showEdgeLines);
            handleSize = EditorGUILayout.Slider("Node Gizmo Size", handleSize, 0.1f, 1.0f);
        }
        EditorGUILayout.EndVertical();

        // Section: Quick Scene Selection Connector
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Connect Selected Scene Nodes", EditorStyles.boldLabel);
        DrawSelectionConnectBox();

        // Section: Graph Health & Disconnected Nodes
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Graph Health & Overview", EditorStyles.boldLabel);
        DrawGraphHealthBox();

        // Section: Route Tester
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Interactive Route Preview", EditorStyles.boldLabel);
        DrawRouteTesterBox();

        // Section: Batch & Sync Utilities
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Automations & Syncing", EditorStyles.boldLabel);
        DrawAutomationsBox();

        EditorGUILayout.Space(12);
        EditorGUILayout.EndScrollView();
    }

    private void DrawSelectionConnectBox()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        var selectedGos = Selection.gameObjects;
        var selectedNodeIds = new List<string>();

        foreach (var go in selectedGos)
        {
            string id = CleanNodeId(go.name);
            EnsureNodeInGraph(go);
            if (graph.nodes.Exists(n => n.Id == id))
            {
                selectedNodeIds.Add(id);
            }
        }

        if (selectedNodeIds.Count == 2)
        {
            string idA = selectedNodeIds[0];
            string idB = selectedNodeIds[1];
            var nodeA = graph.Get(idA);
            var nodeB = graph.Get(idB);

            float dist = Vector3.Distance(nodeA.Position, nodeB.Position);
            bool isConnected = graph.edges.Exists(e => e.From == idA && e.To == idB);
            bool isStairs = nodeA.IsTransition || nodeB.IsTransition || Mathf.Abs(nodeA.Position.y - nodeB.Position.y) > 0.8f;

            EditorGUILayout.LabelField($"Selected: {idA}  <--->  {idB}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Calculated Physical 3D Distance: {dist:F2} meters");
            EditorGUILayout.LabelField($"Status: {(isConnected ? "CONNECTED" : "NOT CONNECTED")}", isConnected ? EditorStyles.boldLabel : EditorStyles.label);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = isConnected ? new Color(0.7f, 1f, 0.7f) : Color.white;
            if (GUILayout.Button(isConnected ? "Update Connection" : "Connect (Bidirectional)", GUILayout.Height(30)))
            {
                ConnectNodes(idA, idB, dist, isStairs);
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (isConnected && GUILayout.Button("Disconnect", GUILayout.Height(30)))
            {
                DisconnectNodes(idA, idB);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Select exactly TWO node GameObjects in the Scene or Hierarchy to quickly connect/disconnect them with automatic physical distance.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawGraphHealthBox()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        int totalNodes = graph.nodes.Count;
        int totalEdges = graph.edges.Count;

        var edgeCount = new Dictionary<string, int>();
        foreach (var n in graph.nodes) edgeCount[n.Id] = 0;
        foreach (var e in graph.edges)
        {
            if (edgeCount.ContainsKey(e.From)) edgeCount[e.From]++;
        }

        var disconnected = new List<string>();
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 0) disconnected.Add(kvp.Key);
        }

        EditorGUILayout.LabelField($"Total Nodes: {totalNodes} | Total Directed Edges: {totalEdges} ({totalEdges / 2} pairs)");

        if (disconnected.Count == 0)
        {
            EditorGUILayout.HelpBox("All nodes are connected to at least one edge in the graph!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Warning: {disconnected.Count} node(s) have ZERO edge connections and cannot be navigated to or from!", MessageType.Error);

            disconnectedScroll = EditorGUILayout.BeginScrollView(disconnectedScroll, GUILayout.Height(80));
            foreach (var discId in disconnected)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(discId, GUILayout.Width(140));
                if (GUILayout.Button("Select in Scene", GUILayout.Width(110)))
                {
                    SelectNodeInScene(discId);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRouteTesterBox()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (graph.nodes.Count > 0)
        {
            var nodeNames = graph.nodes.Select(n => n.Id).ToArray();
            startNodeIdx = Mathf.Clamp(startNodeIdx, 0, nodeNames.Length - 1);
            endNodeIdx = Mathf.Clamp(endNodeIdx, 0, nodeNames.Length - 1);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Start:", GUILayout.Width(45));
            startNodeIdx = EditorGUILayout.Popup(startNodeIdx, nodeNames);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("End:", GUILayout.Width(45));
            endNodeIdx = EditorGUILayout.Popup(endNodeIdx, nodeNames);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Find & Highlight Route in Scene", GUILayout.Height(28)))
            {
                string start = nodeNames[startNodeIdx];
                string end = nodeNames[endNodeIdx];
                previewPath = DijkstraEngine.FindPath(graph, start, end, true, true);
                if (previewPath != null && previewPath.Count > 0)
                {
                    previewDistance = 0f;
                    for (int i = 0; i < previewPath.Count - 1; i++)
                    {
                        var a = graph.Get(previewPath[i]);
                        var b = graph.Get(previewPath[i + 1]);
                        if (a != null && b != null) previewDistance += Vector3.Distance(a.Position, b.Position);
                    }
                    SceneView.RepaintAll();
                }
                else
                {
                    previewPath = new List<string>();
                    previewDistance = 0f;
                    EditorUtility.DisplayDialog("Route Result", $"No valid path found between {start} and {end}.", "OK");
                }
            }

            if (previewPath != null && previewPath.Count > 0)
            {
                EditorGUILayout.HelpBox($"Path Found: {previewPath.Count} waypoints | Total Distance: {previewDistance:F1} m\n{string.Join(" -> ", previewPath)}", MessageType.None);
                if (GUILayout.Button("Clear Highlight", GUILayout.Width(120)))
                {
                    previewPath.Clear();
                    SceneView.RepaintAll();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAutomationsBox()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (GUILayout.Button("Sync Node Coordinates from Scene Hierarchy (NodesRoot)"))
        {
            SyncPositionsFromScene();
        }

        if (GUILayout.Button("Auto-Connect Upper Floor (AT_02 Stairs, AT_03 & AT_04)"))
        {
            AutoConnectUpperFloors();
        }

        if (GUILayout.Button("Export DuplexGraph Directly to SQLite Database"))
        {
            ExportGraphToSqlite();
        }

        if (GUILayout.Button("Re-seed SQLite Database from Scratch"))
        {
            ReseedSqlite();
        }

        EditorGUILayout.EndVertical();
    }

    private void ConnectNodes(string a, string b, float dist, bool isStairs)
    {
        Undo.RecordObject(graph, "Connect Nodes");

        // Ensure both nodes exist in graph.nodes definition list
        if (graph.Get(a) == null)
        {
            var goA = FindNodeGameObject(a);
            if (goA != null) EnsureNodeInGraph(goA);
        }
        if (graph.Get(b) == null)
        {
            var goB = FindNodeGameObject(b);
            if (goB != null) EnsureNodeInGraph(goB);
        }

        // Remove any existing edge between them
        graph.edges.RemoveAll(e => (e.From == a && e.To == b) || (e.From == b && e.To == a));

        // Add fresh bidirectional edges with accurate distance
        graph.edges.Add(new Edge(a, b, dist, isStairs, false));
        graph.edges.Add(new Edge(b, a, dist, isStairs, false));

        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        Debug.Log($"[GraphEditor] Connected {a} <---> {b} (Distance: {dist:F2}m, Stairs: {isStairs})");
    }

    private void DisconnectNodes(string a, string b)
    {
        Undo.RecordObject(graph, "Disconnect Nodes");
        int removed = graph.edges.RemoveAll(e => (e.From == a && e.To == b) || (e.From == b && e.To == a));
        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        Debug.Log($"[GraphEditor] Disconnected {a} and {b} ({removed} edges removed)");
    }

    private void EnsureNodeInGraph(GameObject go)
    {
        if (go == null || graph == null) return;
        string id = CleanNodeId(go.name);
        if (graph.nodes.Exists(n => n.Id == id)) return;

        // Auto-register new node from scene GameObject
        int floor = id.StartsWith("L2_") ? 2 : (id.StartsWith("L1_") ? 1 : (go.transform.position.y > 0.8f ? 2 : 1));
        string area = "AT_01";
        if (go.transform.parent != null && go.transform.parent.name.StartsWith("Nodes_"))
        {
            area = go.transform.parent.name.Replace("Nodes_", "");
        }
        else if (id.Contains("AT02") || id.Contains("AT_02")) area = "AT_02";
        else if (id.Contains("AT03") || id.Contains("AT_03")) area = "AT_03";
        else if (id.Contains("AT04") || id.Contains("AT_04")) area = "AT_04";

        bool isTrans = go.name.ToLower().Contains("stair") || go.name.ToLower().Contains("entry") || go.name.ToLower().Contains("exit");

        Undo.RecordObject(graph, "Auto Add Node to Graph");
        graph.nodes.Add(new Node(id, go.transform.position, floor, area, isTrans));
        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        Debug.Log($"[NavGraphEditor] Automatically registered new node: {id} at {go.transform.position} (Floor: {floor}, Area: {area}, Transition: {isTrans})");
    }

    private GameObject FindNodeGameObject(string nodeId)
    {
        if (nodesRoot == null) nodesRoot = GameObject.Find("NodesRoot");
        if (nodesRoot != null)
        {
            foreach (Transform group in nodesRoot.transform)
            {
                foreach (Transform child in group)
                {
                    if (CleanNodeId(child.name) == nodeId) return child.gameObject;
                }
            }
        }
        return GameObject.Find("Node_" + nodeId) ?? GameObject.Find(nodeId);
    }

    private void SyncPositionsFromScene()
    {
        if (nodesRoot == null) nodesRoot = GameObject.Find("NodesRoot");
        if (nodesRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "NodesRoot GameObject not found in current scene.", "OK");
            return;
        }

        Undo.RecordObject(graph, "Sync Scene Node Positions");
        int updated = 0;

        foreach (Transform group in nodesRoot.transform)
        {
            string area = group.name.Replace("Nodes_", "");
            foreach (Transform child in group)
            {
                string id = CleanNodeId(child.name);
                var node = graph.Get(id);
                int floor = id.StartsWith("L2_") ? 2 : (id.StartsWith("L1_") ? 1 : (child.position.y > 0.8f ? 2 : 1));

                if (node != null)
                {
                    node.Position = child.position;
                    node.Floor = floor;
                    node.AuthoritativeArea = area;
                    updated++;
                }
                else
                {
                    // Add new node found in scene
                    bool isTrans = child.name.ToLower().Contains("stair") || child.name.ToLower().Contains("entry") || child.name.ToLower().Contains("exit");
                    graph.nodes.Add(new Node(id, child.position, floor, area, isTrans));
                    updated++;
                }
            }
        }

        // Recalculate edge distances based on updated positions
        foreach (var e in graph.edges)
        {
            var na = graph.Get(e.From);
            var nb = graph.Get(e.To);
            if (na != null && nb != null)
            {
                e.Cost = Vector3.Distance(na.Position, nb.Position);
            }
        }

        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("Sync Complete", $"Successfully synced {updated} nodes and refreshed all edge distances from Scene transforms.", "OK");
    }

    private void AutoConnectUpperFloors()
    {
        Undo.RecordObject(graph, "Auto Connect Upper Floors");

        var pairs = new (string a, string b)[] {
            ("L2_AT03_ENTRY", "L2_T3"),
            ("L2_T3", "L2_CorA"),
            ("L2_CorA", "L2_CorB"),
            ("L2_CorA", "L2_R206"),
            ("L2_CorA", "L2_R208"),
            ("L2_CorB", "L2_R207"),
            ("L2_CorB", "L2_R208"),
            ("L2_AT03_ENTRY", "L2_AT04_ENTRY"),
            ("L2_T3", "L2_AT04_ENTRY"),
            ("L2_AT04_ENTRY", "L2_T4"),
            ("L2_T4", "L2_CorC"),
            ("L2_CorC", "L2_CorD"),
            ("L2_CorC", "L2_R205"),
            ("L2_CorD", "L2_R200"),
            ("L2_CorD", "L2_R202"),
            ("L2_AT03_EXIT", "L2_AT04_EXIT"),
            ("L2_CorA", "L2_AT03_EXIT"),
            ("L2_CorC", "L2_AT04_EXIT"),
            ("L2_AT04_ENTRY", "L2_AT04_EXIT"),
            ("L2_AT03_ENTRY", "L2_AT03_EXIT"),
            ("L2_T3", "L2_AT03_EXIT"),
            ("L2_T4", "L2_AT04_EXIT")
        };

        int count = 0;
        foreach (var p in pairs)
        {
            var na = graph.Get(p.a);
            var nb = graph.Get(p.b);
            if (na == null || nb == null) continue;
            float dist = Vector3.Distance(na.Position, nb.Position);
            ConnectNodes(p.a, p.b, dist, false);
            count++;
        }

        EditorUtility.SetDirty(graph);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("Auto-Connect Complete", $"Connected {count} upper floor corridors and room pairs.", "OK");
    }

    private void ExportGraphToSqlite()
    {
        if (graph == null) return;

        var dbMgr = Object.FindFirstObjectByType<DatabaseManager>();
        if (dbMgr == null)
        {
            EditorUtility.DisplayDialog("Error", "DatabaseManager not found in scene.", "OK");
            return;
        }

        try
        {
            using (var conn = dbMgr.OpenConn())
            {
                if (conn == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not open SQLite database connection.", "OK");
                    return;
                }

                // 1. Clear old graph elements
                conn.Exec("DELETE FROM Navigation_Edges;");
                conn.Exec("DELETE FROM Navigation_Nodes;");

                // 2. Insert all nodes from DuplexGraph
                var ids = new Dictionary<string, int>();
                foreach (var n in graph.nodes)
                {
                    var sql = $"INSERT INTO Navigation_Nodes (node_name,x_coordinate,y_coordinate,z_coordinate,node_type,floor,authoritative_area,is_transition) VALUES ('{n.Id}',{n.Position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},{n.Position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)},{n.Position.z.ToString(System.Globalization.CultureInfo.InvariantCulture)},'junction',{n.Floor},'{n.AuthoritativeArea}',{(n.IsTransition ? 1 : 0)});";
                    conn.Exec(sql);
                    ids[n.Id] = DbSeeder.Nid(conn, n.Id);
                }

                // 3. Insert all edges from DuplexGraph
                int edgeCount = 0;
                foreach (var e in graph.edges)
                {
                    if (ids.ContainsKey(e.From) && ids.ContainsKey(e.To))
                    {
                        var sql = $"INSERT INTO Navigation_Edges (start_node_id,end_node_id,distance,is_walkable,is_stairs,is_elevator) VALUES ({ids[e.From]},{ids[e.To]},{e.Cost.ToString(System.Globalization.CultureInfo.InvariantCulture)},1,{(e.IsStairs ? 1 : 0)},0);";
                        conn.Exec(sql);
                        edgeCount++;
                    }
                }

                // 4. Seed locations directly from DbQueries.StaticLocations
                DbSeeder.SeedLocations(conn);

                EditorUtility.DisplayDialog("Export Complete", $"Successfully exported {graph.nodes.Count} nodes and {edgeCount} edges from DuplexGraph to SQLite!", "OK");
                Debug.Log($"[NavGraphEditor] Exported {graph.nodes.Count} nodes and {edgeCount} edges to SQLite database.");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Export Error", ex.Message, "OK");
        }
    }

    private void ReseedSqlite()
    {
        string dbPath = System.IO.Path.Combine(Application.persistentDataPath, "nexpath.db");
        if (System.IO.File.Exists(dbPath))
        {
            System.IO.File.Delete(dbPath);
        }

        var dbMgr = Object.FindFirstObjectByType<DatabaseManager>();
        if (dbMgr != null)
        {
            dbMgr.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        EditorUtility.DisplayDialog("Database Re-seeded", "Deleted cached SQLite database. On next play or run, it will reseed with the latest node and edge data.", "OK");
    }

    private void SelectNodeInScene(string nodeId)
    {
        if (nodesRoot == null) nodesRoot = GameObject.Find("NodesRoot");
        if (nodesRoot != null)
        {
            foreach (Transform group in nodesRoot.transform)
            {
                foreach (Transform child in group)
                {
                    if (CleanNodeId(child.name) == nodeId)
                    {
                        Selection.activeGameObject = child.gameObject;
                        SceneView.FrameLastActiveSceneView();
                        return;
                    }
                }
            }
        }
    }

    private string CleanNodeId(string raw)
    {
        return raw.Replace("Node_", "").Trim();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showSceneVisuals || graph == null || graph.nodes == null) return;

        // 1. Draw Edges
        if (showEdgeLines && graph.edges != null)
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            foreach (var e in graph.edges)
            {
                var a = graph.Get(e.From);
                var b = graph.Get(e.To);
                if (a == null || b == null) continue;

                if (e.IsStairs)
                {
                    Handles.color = new Color(1f, 0.85f, 0.2f, 0.75f);
                    Handles.DrawDottedLine(a.Position, b.Position, 4f);
                }
                else
                {
                    Handles.color = a.Floor == 1 ? new Color(0.2f, 0.9f, 0.3f, 0.45f) : new Color(0.2f, 0.7f, 1f, 0.45f);
                    Handles.DrawLine(a.Position, b.Position, 2f);
                }
            }
        }

        // 2. Draw Nodes
        var edgeCount = new Dictionary<string, int>();
        foreach (var n in graph.nodes) edgeCount[n.Id] = 0;
        foreach (var e in graph.edges) if (edgeCount.ContainsKey(e.From)) edgeCount[e.From]++;

        foreach (var n in graph.nodes)
        {
            bool isDisconnected = edgeCount.ContainsKey(n.Id) && edgeCount[n.Id] == 0;

            if (isDisconnected)
                Handles.color = Color.red;
            else if (n.IsTransition || n.Id.Contains("STAIR"))
                Handles.color = new Color(1f, 0.85f, 0.1f);
            else if (n.Floor == 1)
                Handles.color = new Color(0.2f, 0.9f, 0.3f);
            else
                Handles.color = new Color(0.2f, 0.7f, 1f);

            Handles.SphereHandleCap(0, n.Position, Quaternion.identity, handleSize, EventType.Repaint);

            if (showNodeLabels)
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
                labelStyle.normal.textColor = Handles.color;
                labelStyle.fontSize = 11;
                Handles.Label(n.Position + Vector3.up * (handleSize * 0.9f), n.Id, labelStyle);
            }
        }

        // 3. Draw Highlighted Test Route
        if (previewPath != null && previewPath.Count > 1)
        {
            Handles.color = new Color(1f, 0.84f, 0f, 1f);
            for (int i = 0; i < previewPath.Count - 1; i++)
            {
                var a = graph.Get(previewPath[i]);
                var b = graph.Get(previewPath[i + 1]);
                if (a != null && b != null)
                {
                    Handles.DrawLine(a.Position, b.Position, 5f);
                    Handles.SphereHandleCap(0, a.Position, Quaternion.identity, handleSize * 1.3f, EventType.Repaint);
                }
            }
            var last = graph.Get(previewPath.Last());
            if (last != null)
            {
                Handles.SphereHandleCap(0, last.Position, Quaternion.identity, handleSize * 1.5f, EventType.Repaint);
            }
        }
    }
}
