using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using TMPro;

public class CreateEditorBuildingMeshes : EditorWindow
{
    [MenuItem("ARNav/Testing/Create Editor Building Meshes")]
    public static void ShowWindow()
    {
        GetWindow<CreateEditorBuildingMeshes>("Create Building Meshes");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Editor-Only Building Visualization", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("Creates simple floor/wall meshes visible in Game View during Editor Play mode.", EditorStyles.wordWrappedLabel);
        GUILayout.Label("These are ONLY for Editor testing — not included in builds.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Building Meshes", GUILayout.Height(40)))
        {
            CreateMeshes();
        }

        if (GUILayout.Button("Remove Building Meshes", GUILayout.Height(30)))
        {
            RemoveMeshes();
        }
    }

    static void CreateMeshes()
    {
        var parent = GameObject.Find("EditorBuildingVisualization");
        if (parent == null)
        {
            parent = new GameObject("EditorBuildingVisualization");
            parent.transform.position = Vector3.zero;
        }
        else
        {
            while (parent.transform.childCount > 0)
                DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }

        // Tag as EditorOnly so it's stripped from builds
        parent.tag = "EditorOnly";

        // Create a simple unlit material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = new Color(0.8f, 0.8f, 0.85f, 0.3f); // Semi-transparent gray-blue
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;

        var matWalls = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        matWalls.color = new Color(0.6f, 0.65f, 0.7f, 0.4f);
        matWalls.SetFloat("_Surface", 1);
        matWalls.SetFloat("_Blend", 0);
        matWalls.SetFloat("_ZWrite", 0);
        matWalls.renderQueue = 3000;

        // Floor 1 bounds (from AT_01 bbox: x: 3.26 to 22.06, z: -24.40 to 1.10, y: -1.99 to 2.20)
        CreateFloor(parent, "Floor_1", 
            new Vector3(12.66f, -1.5f, -11.65f), 
            new Vector3(18.8f, 0.1f, 25.5f), 
            mat);

        // Floor 2 bounds (from AT_03/04: x: -15.7 to 19.0, z: -15.6 to 3.69, y: -6.07 to 2.78)
        // Approximate: two areas combined
        CreateFloor(parent, "Floor_2_Left", 
            new Vector3(-3.5f, 1.5f, -9.5f), 
            new Vector3(25f, 0.1f, 12f), 
            mat);
        CreateFloor(parent, "Floor_2_Right", 
            new Vector3(8.5f, 1.5f, -9.5f), 
            new Vector3(20f, 0.1f, 12f), 
            mat);

        // Staircase floor
        CreateFloor(parent, "Staircase", 
            new Vector3(8.5f, -1.5f, -13.5f), 
            new Vector3(12f, 0.1f, 8f), 
            mat);

        // Walls for Floor 1 (simple perimeter)
        CreateWall(parent, "Wall_F1_North", new Vector3(12.66f, 0f, 1.1f), new Vector3(18.8f, 4f, 0.2f), matWalls);
        CreateWall(parent, "Wall_F1_South", new Vector3(12.66f, 0f, -24.4f), new Vector3(18.8f, 4f, 0.2f), matWalls);
        CreateWall(parent, "Wall_F1_East", new Vector3(22.06f, 0f, -11.65f), new Vector3(0.2f, 4f, 25.5f), matWalls);
        CreateWall(parent, "Wall_F1_West", new Vector3(3.26f, 0f, -11.65f), new Vector3(0.2f, 4f, 25.5f), matWalls);

        // Corridor walls (internal)
        CreateWall(parent, "Wall_Corridor_Main", new Vector3(12.66f, 0f, -14f), new Vector3(18.8f, 3f, 0.15f), matWalls);

        // Room divider hints (thin lines)
        CreateRoomDividers(parent, matWalls);

        // Staircase walls
        CreateWall(parent, "Wall_Stairs_North", new Vector3(8.5f, 0f, -9f), new Vector3(12f, 4f, 0.15f), matWalls);
        CreateWall(parent, "Wall_Stairs_South", new Vector3(8.5f, 0f, -18f), new Vector3(12f, 4f, 0.15f), matWalls);

        // Stair indicator
        CreateStairSymbol(parent, new Vector3(8.5f, -1.3f, -13.5f));

        Debug.Log("[CreateEditorBuildingMeshes] Created Editor-only building visualization under 'EditorBuildingVisualization'");
        EditorUtility.SetDirty(parent);
        SceneView.RepaintAll();
    }

    static void CreateFloor(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = center;
        go.tag = "EditorOnly";

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = CreateBoxMesh(size);

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static void CreateWall(GameObject parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = center;
        go.tag = "EditorOnly";

        var mf = go.AddComponent<MeshFilter>();
        mf.mesh = CreateBoxMesh(size);

        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Mesh CreateBoxMesh(Vector3 size)
    {
        var mesh = new Mesh();
        float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;

        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz),
            new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz),
            new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz),
            new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz)
        };

        int[] triangles = new int[36]
        {
            0, 2, 1, 0, 3, 2, // back
            5, 6, 7, 5, 7, 4, // front
            4, 7, 3, 4, 3, 0, // left
            1, 2, 6, 1, 6, 5, // right
            3, 7, 6, 3, 6, 2, // top
            4, 0, 1, 4, 1, 5  // bottom
        };

        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
            uvs[i] = new Vector2(vertices[i].x / size.x + 0.5f, vertices[i].z / size.z + 0.5f);

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void CreateRoomDividers(GameObject parent, Material mat)
    {
        // Thin lines showing room boundaries at known positions
        var dividerMat = new Material(mat);
        dividerMat.color = new Color(1f, 1f, 1f, 0.2f);

        // Floor 1 room dividers (at x=4.3, 8.37, etc.)
        float[] f1X = { 4.3f, 8.37f, 10.11f, 14.24f, 17.51f, 13.13f };
        foreach (float x in f1X)
        {
            CreateWall(parent, $"Divider_F1_{x}", new Vector3(x, -1.3f, -21.2f), new Vector3(0.05f, 3f, 18f), dividerMat);
        }
    }

    static void CreateStairSymbol(GameObject parent, Vector3 center)
    {
        var go = new GameObject("StairSymbol");
        go.transform.SetParent(parent.transform);
        go.transform.position = center;
        go.tag = "EditorOnly";

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "🡹 STAIRS 🡹";
        tmp.fontSize = 1.5f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    static void RemoveMeshes()
    {
        var parent = GameObject.Find("EditorBuildingVisualization");
        if (parent != null)
            DestroyImmediate(parent);
        else
            Debug.Log("[CreateEditorBuildingMeshes] No EditorBuildingVisualization found");
    }
}