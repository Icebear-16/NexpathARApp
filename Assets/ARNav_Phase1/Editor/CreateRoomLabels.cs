using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CreateRoomLabels : EditorWindow
{
    [MenuItem("ARNav/Testing/Create Room Labels for Vuforia")]
    public static void ShowWindow()
    {
        GetWindow<CreateRoomLabels>("Create Room Labels");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create Room Labels for Vuforia Visual Tracking", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("This will create TextMeshPro labels at each room's node position.", EditorStyles.wordWrappedLabel);
        GUILayout.Label("Rooms sharing the same node position will use the first room name.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Labels", GUILayout.Height(40)))
        {
            CreateLabels();
        }

        if (GUILayout.Button("Remove All Labels", GUILayout.Height(30)))
        {
            RemoveLabels();
        }
    }

    static void CreateLabels()
    {
        // Map of nodeId -> (roomName, position, floor)
        var roomNodes = new Dictionary<string, (string roomName, Vector3 position, int floor)>
        {
            // Ground Floor (Floor 1, y ≈ -1.5)
            { "L1_R101", ("Room 101", new Vector3(4.3094f, -1.5f, -22.331259f), 1) },
            { "L1_R100", ("Room 100", new Vector3(4.3094f, -1.5f, -22.331259f), 1) }, // Same as 101
            { "L1_R103", ("Room 103", new Vector3(8.3654f, -1.5f, -20.07626f), 1) },
            { "L1_R104", ("Room 104", new Vector3(8.3654f, -1.5f, -20.07626f), 1) }, // Same as 103
            { "L1_R105", ("Room 105", new Vector3(8.3654f, -1.5f, -20.07626f), 1) }, // Same as 103
            { "L1_R106", ("Room 106", new Vector3(10.1124f, -1.5f, -16.93526f), 1) },
            { "L1_R107", ("Room 107", new Vector3(14.2444f, -1.5f, -10.27526f), 1) },
            { "L1_R108", ("Room 108", new Vector3(17.5104f, -1.5f, -5.31126f), 1) },
            { "L1_R109", ("Room 109", new Vector3(13.1334f, -1.5f, -9.02426f), 1) },

            // Upper Floor (Floor 2, y ≈ 1.5-1.65)
            { "L2_R200", ("Room 200", new Vector3(4.3094f, 1.5f, -22.331259f), 2) },
            { "L2_R202", ("Room 202", new Vector3(6.2544003f, 1.5f, -23.01326f), 2) },
            { "L2_R205", ("Room 205", new Vector3(9.0544f, 1.5f, -18.37326f), 2) },
            { "L2_R206", ("Room 206", new Vector3(14.2444f, 1.65f, -10.27526f), 2) },
            { "L2_R207", ("Room 207", new Vector3(17.5104f, 1.65f, -5.31126f), 2) },
            { "L2_R208", ("Room 208", new Vector3(13.3234f, 1.65f, -9.46026f), 2) },
        };

        // Group by position, keep first room name
        var positionToLabel = new Dictionary<Vector3, (string label, int floor)>();
        foreach (var kvp in roomNodes)
        {
            var pos = kvp.Value.position;
            var key = new Vector3(Mathf.Round(pos.x * 1000f), Mathf.Round(pos.y * 1000f), Mathf.Round(pos.z * 1000f));
            if (!positionToLabel.ContainsKey(key))
            {
                positionToLabel[key] = (kvp.Value.roomName, kvp.Value.floor);
            }
        }

        // Find or create parent container
        var parent = GameObject.Find("RoomLabels_VuforiaTest");
        if (parent == null)
        {
            parent = new GameObject("RoomLabels_VuforiaTest");
            parent.transform.position = Vector3.zero;
        }
        else
        {
            // Clear existing
            while (parent.transform.childCount > 0)
            {
                DestroyImmediate(parent.transform.GetChild(0).gameObject);
            }
        }

        // Get default TMP font asset
        var fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
            // Try to find any TMP font asset
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        int created = 0;
        foreach (var kvp in positionToLabel)
        {
            var pos = new Vector3(kvp.Key.x / 1000f, kvp.Key.y / 1000f, kvp.Key.z / 1000f);
            var labelText = kvp.Value.label;
            var floor = kvp.Value.floor;

            var go = new GameObject($"Label_{labelText}_F{floor}");
            go.transform.SetParent(parent.transform);
            go.transform.position = pos + Vector3.up * 0.3f; // Slightly above floor

            // Add TextMeshPro
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.fontSize = 0.5f;
            tmp.color = floor == 1 ? Color.cyan : Color.yellow;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            if (fontAsset != null) tmp.font = fontAsset;

            // Make it face camera (Billboard)
            var billboard = go.AddComponent<BillboardText>();
            billboard.targetCamera = Camera.main;

            // Note: 3D TextMeshPro doesn't support UI Image background
            // For readability, the text color + billboard is sufficient

            created++;
        }

        Debug.Log($"[CreateRoomLabels] Created {created} room labels under '{parent.name}'");
        EditorUtility.SetDirty(parent);
        SceneView.RepaintAll();
    }

    static void RemoveLabels()
    {
        var parent = GameObject.Find("RoomLabels_VuforiaTest");
        if (parent != null)
        {
            DestroyImmediate(parent);
            Debug.Log("[CreateRoomLabels] Removed all room labels");
        }
        else
        {
            Debug.Log("[CreateRoomLabels] No RoomLabels_VuforiaTest object found");
        }
    }
}

