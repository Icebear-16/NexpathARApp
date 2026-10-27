using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SetupSearchDropdownAndLabels : EditorWindow
{
    [MenuItem("ARNav/Testing/🚀 Complete Setup (Dropdown + Labels)")]
    public static void ShowWindow()
    {
        GetWindow<SetupSearchDropdownAndLabels>("Complete Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Complete Search Dropdown + Room Labels Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Label("This will:", EditorStyles.wordWrappedLabel);
        GUILayout.Label("1. Create SearchResultItem prefab", EditorStyles.wordWrappedLabel);
        GUILayout.Label("2. Create SearchResultsScrollView under SearchPopupPanel", EditorStyles.wordWrappedLabel);
        GUILayout.Label("3. Wire Results Content + Prefab to AppUIController on Canvas", EditorStyles.wordWrappedLabel);
        GUILayout.Label("4. Create all 12 Room Labels for Vuforia testing", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Run Complete Setup", GUILayout.Height(50)))
        {
            RunCompleteSetup();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Remove Search Dropdown Setup", GUILayout.Height(30)))
        {
            RemoveDropdownSetup();
        }
        if (GUILayout.Button("Remove Room Labels", GUILayout.Height(30)))
        {
            RemoveLabels();
        }
    }

    static void RunCompleteSetup()
    {
        // Step 1: Find Canvas with AppUIController
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[Setup] Canvas not found in scene!");
            return;
        }
        var uiController = canvas.GetComponent<AppUIController>();
        if (uiController == null)
        {
            Debug.LogError("[Setup] AppUIController not found on Canvas!");
            return;
        }

        // Step 2: Find SearchPopupPanel
        var searchPopup = GameObject.Find("SearchPopupPanel");
        if (searchPopup == null)
        {
            Debug.LogError("[Setup] SearchPopupPanel not found in scene!");
            return;
        }

        // Step 3: Create SearchResultItem Prefab
        var prefab = CreateResultItemPrefab();
        if (prefab == null) return;

        // Step 4: Create SearchResultsScrollView under SearchPopupPanel
        var scrollView = CreateSearchResultsScrollView(searchPopup);
        if (scrollView == null) return;

        // Step 5: Get Content transform
        var content = scrollView.transform.Find("Viewport/Content");
        if (content == null)
        {
            Debug.LogError("[Setup] Content not found in ScrollView!");
            return;
        }

        // Step 6: Assign to AppUIController
        var so = new SerializedObject(uiController);
        var contentProp = so.FindProperty("resultsContent");
        var prefabProp = so.FindProperty("resultItemPrefab");
        contentProp.objectReferenceValue = content;
        prefabProp.objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        // Step 7: Mark scene dirty
        EditorUtility.SetDirty(uiController);
        EditorUtility.SetDirty(searchPopup);

        // Step 8: Create Room Labels
        CreateAllRoomLabels();

        Debug.Log("[Setup] ✅ Complete! Search dropdown wired + 12 room labels created.");
        SceneView.RepaintAll();
    }

    static GameObject CreateResultItemPrefab()
    {
        // Create prefab directory
        string prefabDir = "Assets/ARNav_Phase1/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
            AssetDatabase.CreateFolder("Assets/ARNav_Phase1", "Prefabs");

        // Create the prefab GameObject
        var go = new GameObject("SearchResultItem");
        go.layer = 5; // UI layer

        // Add RectTransform (required for UI elements)
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 60);

        // Add Button
        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f),
            highlightedColor = new Color(0.3f, 0.5f, 0.8f, 0.9f),
            pressedColor = new Color(0.1f, 0.3f, 0.6f, 1f),
            selectedColor = new Color(0.3f, 0.5f, 0.8f, 0.9f),
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        // Add background Image (target graphic)
        var bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        bgImg.sprite = null;
        bgImg.type = Image.Type.Sliced;
        btn.targetGraphic = bgImg;

        // Add LayoutElement for consistent height
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 60;
        le.flexibleWidth = 1;

        // Create Label child
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform);
        labelGo.layer = 5;
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(15, 5);
        labelRect.offsetMax = new Vector2(-15, -5);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "Room Name (Category)";
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20;
        tmp.fontSizeMax = 32;
        tmp.raycastTarget = false;

        // Try to find a TMP font asset
        var fontGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (fontGuids.Length > 0)
        {
            var fontPath = AssetDatabase.GUIDToAssetPath(fontGuids[0]);
            tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        }

        // Save as prefab
        string prefabPath = "Assets/ARNav_Phase1/Prefabs/SearchResultItem.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log("[Setup] Created SearchResultItem prefab at " + prefabPath);
        return prefab;
    }

    static GameObject CreateSearchResultsScrollView(GameObject parent)
    {
        // Remove existing if present
        var existing = parent.transform.Find("SearchResultsScrollView");
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        // Create ScrollView
        var scrollView = new GameObject("SearchResultsScrollView");
        scrollView.transform.SetParent(parent.transform);
        scrollView.layer = 5;

        var scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 30;

        // RectTransform: anchor bottom, below filter buttons
        var svRect = scrollView.GetComponent<RectTransform>();
        svRect.anchorMin = new Vector2(0, 0);
        svRect.anchorMax = new Vector2(1, 0);
        svRect.pivot = new Vector2(0.5f, 0);
        svRect.anchoredPosition = new Vector2(0, 10);
        svRect.sizeDelta = new Vector2(-20, 300);

        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        viewport.layer = 5;
        var vpRect = viewport.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        vpRect.pivot = new Vector2(0.5f, 0.5f);
        var vpMask = viewport.AddComponent<RectMask2D>();

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        content.layer = 5;
        var cRect = content.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 0);

        // Vertical Layout Group
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.spacing = 5;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        // Content Size Fitter
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire ScrollRect
        scrollRect.content = cRect;
        scrollRect.viewport = vpRect;

        Debug.Log("[Setup] Created SearchResultsScrollView under SearchPopupPanel");
        return scrollView;
    }

    static void CreateAllRoomLabels()
    {
        // Map of nodeId -> (roomName, position, floor)
        var roomNodes = new Dictionary<string, (string roomName, Vector3 position, int floor)>
        {
            { "L1_R101", ("Room 101", new Vector3(4.3094f, -1.5f, -22.331259f), 1) },
            { "L1_R100", ("Room 100", new Vector3(4.3094f, -1.5f, -22.331259f), 1) },
            { "L1_R103", ("Room 103", new Vector3(8.3654f, -1.5f, -20.07626f), 1) },
            { "L1_R104", ("Room 104", new Vector3(8.3654f, -1.5f, -20.07626f), 1) },
            { "L1_R105", ("Room 105", new Vector3(8.3654f, -1.5f, -20.07626f), 1) },
            { "L1_R106", ("Room 106", new Vector3(10.1124f, -1.5f, -16.93526f), 1) },
            { "L1_R107", ("Room 107", new Vector3(14.2444f, -1.5f, -10.27526f), 1) },
            { "L1_R108", ("Room 108", new Vector3(17.5104f, -1.5f, -5.31126f), 1) },
            { "L1_R109", ("Room 109", new Vector3(13.1334f, -1.5f, -9.02426f), 1) },
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

        // Find or create parent
        var parent = GameObject.Find("RoomLabels_VuforiaTest");
        if (parent == null)
        {
            parent = new GameObject("RoomLabels_VuforiaTest");
            parent.transform.position = Vector3.zero;
        }
        else
        {
            while (parent.transform.childCount > 0)
                DestroyImmediate(parent.transform.GetChild(0).gameObject);
        }

        // Get default TMP font asset
        var fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fontAsset == null)
        {
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
            go.transform.position = pos + Vector3.up * 0.3f;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.fontSize = 0.5f;
            tmp.color = floor == 1 ? Color.cyan : Color.yellow;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            if (fontAsset != null) tmp.font = fontAsset;

            // Billboard component (runtime)
            var billboard = go.AddComponent<BillboardText>();
            billboard.targetCamera = Camera.main;

            // Note: 3D TextMeshPro doesn't support UI Image background
            // For readability, the text color + billboard is sufficient

            created++;
        }

        Debug.Log($"[Setup] Created {created} room labels under 'RoomLabels_VuforiaTest'");
    }

    static void RemoveDropdownSetup()
    {
        var searchPopup = GameObject.Find("SearchPopupPanel");
        if (searchPopup != null)
        {
            var existing = searchPopup.transform.Find("SearchResultsScrollView");
            if (existing != null)
                DestroyImmediate(existing.gameObject);
        }
        Debug.Log("[Setup] Removed SearchResultsScrollView");
    }

    static void RemoveLabels()
    {
        var parent = GameObject.Find("RoomLabels_VuforiaTest");
        if (parent != null)
            DestroyImmediate(parent);
        else
            Debug.Log("[Setup] No RoomLabels_VuforiaTest found");
    }
}