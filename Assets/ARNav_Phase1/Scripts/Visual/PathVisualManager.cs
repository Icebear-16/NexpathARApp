using UnityEngine;
using System.Collections.Generic;

public class PathVisualManager : MonoBehaviour
{
    public static PathVisualManager I
    {
        get
        {
            if (_i == null) _i = Object.FindAnyObjectByType<PathVisualManager>();
            return _i;
        }
        set { _i = value; }
    }
    private static PathVisualManager _i;

    public enum PathStyle
    {
        AnimatedArrows,
        ClassicLine
    }

    [Header("Line Settings")]
    public LineRenderer line;
    public float heightOffset = 0.08f;   // 8cm above floor to stay cleanly visible above ground plane
    public float scrollSpeed = 1.3f;    // Pacing of animated chevrons

    [Header("Path Styles & Materials")]
    public PathStyle currentStyle = PathStyle.AnimatedArrows;
    public Material arrowMaterial;
    public Material classicMaterial;
    public float arrowWidth = 0.40f;
    public float classicWidth = 0.20f;

    public System.Action<PathStyle> OnPathStyleChanged;

    private string currentDestinationName = "";
    private List<Vector3> cachedPath = new();

    void Awake()
    {
        I = this;
        EnsureReferences();
    }

    void EnsureReferences()
    {
        if (line == null) 
        {
            var go = GameObject.Find("PathRenderer");
            if (go != null) line = go.GetComponent<LineRenderer>();
        }

        // Auto-load materials if not assigned
        if (arrowMaterial == null)
        {
            arrowMaterial = Resources.Load<Material>("LineRenderMat_Arrows");
            if (arrowMaterial == null)
            {
#if UNITY_EDITOR
                arrowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/ARNav_Phase1/Materials/LineRenderMat_Arrows.mat");
#endif
            }
        }

        if (classicMaterial == null)
        {
            classicMaterial = Resources.Load<Material>("LineRenderMat_Classic");
            if (classicMaterial == null)
            {
#if UNITY_EDITOR
                classicMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/ARNav_Phase1/Materials/LineRenderMat_Classic.mat");
#endif
            }
        }

        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 0;
            ApplyStyleSettings();
        }

        // Ensure DestinationWaypointMarker exists in the scene
        if (DestinationWaypointMarker.I == null)
        {
            var markerGo = new GameObject("DestinationWaypointMarker");
            markerGo.AddComponent<DestinationWaypointMarker>();
        }
    }

    public void TogglePathStyle()
    {
        currentStyle = currentStyle == PathStyle.AnimatedArrows ? PathStyle.ClassicLine : PathStyle.AnimatedArrows;
        ApplyStyleSettings();

        // Re-render cached path with new material/width if active
        if (cachedPath != null && cachedPath.Count > 0)
        {
            Render(cachedPath);
        }

        OnPathStyleChanged?.Invoke(currentStyle);
        Debug.Log($"[PathVisualManager] Switched path style to: {currentStyle}");
    }

    public void SetPathStyle(PathStyle style)
    {
        currentStyle = style;
        ApplyStyleSettings();

        if (cachedPath != null && cachedPath.Count > 0)
        {
            Render(cachedPath);
        }

        OnPathStyleChanged?.Invoke(currentStyle);
    }

    void ApplyStyleSettings()
    {
        if (line == null) return;

        if (currentStyle == PathStyle.AnimatedArrows)
        {
            if (arrowMaterial != null) line.material = arrowMaterial;
            line.startWidth = arrowWidth;
            line.endWidth = arrowWidth;
            line.textureMode = LineTextureMode.Tile;
            line.alignment = LineAlignment.View;
        }
        else // ClassicLine
        {
            if (classicMaterial != null) line.material = classicMaterial;
            line.startWidth = classicWidth;
            line.endWidth = classicWidth;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
        }
    }

    public void SetDestinationName(string name)
    {
        currentDestinationName = name;
    }

    public void Render(List<Vector3> path)
    {
        if (line == null) 
        {
            var go = GameObject.Find("PathRenderer");
            if (go != null) line = go.GetComponent<LineRenderer>();
        }
        if (line == null) return;

        if (path == null || path.Count == 0)
        {
            line.positionCount = 0;
            cachedPath.Clear();
            if (DestinationWaypointMarker.I != null) DestinationWaypointMarker.I.Hide();
            return;
        }

        cachedPath = new List<Vector3>(path);
        ApplyStyleSettings();

        // Smooth path using Catmull-Rom
        var smooth = Smooth(path, 8);
        line.positionCount = smooth.Count;
        for (int i = 0; i < smooth.Count; i++)
        {
            line.SetPosition(i, smooth[i] + Vector3.up * heightOffset);
        }
        line.enabled = true;

        // Position Destination Marker at final destination coordinate
        if (DestinationWaypointMarker.I == null)
        {
            var markerGo = new GameObject("DestinationWaypointMarker");
            markerGo.AddComponent<DestinationWaypointMarker>();
        }
        if (DestinationWaypointMarker.I != null && path.Count > 0)
        {
            Vector3 destPos = path[path.Count - 1];
            DestinationWaypointMarker.I.ShowAt(destPos, currentDestinationName);
        }
    }

    public void Clear()
    {
        cachedPath.Clear();
        if (line != null) line.positionCount = 0;
        if (DestinationWaypointMarker.I != null) DestinationWaypointMarker.I.Hide();
    }

    // Catmull-Rom smoothing to avoid sharp 90-degree kinks
    List<Vector3> Smooth(List<Vector3> pts, int samples)
    {
        if (pts == null || pts.Count == 0) return new List<Vector3>();
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

    void Update()
    {
        // Animate arrow UV offset forward along the line only in AnimatedArrows mode
        if (currentStyle == PathStyle.AnimatedArrows && line != null && line.enabled && line.positionCount > 0 && line.material != null)
        {
            line.material.mainTextureOffset -= Vector2.right * scrollSpeed * Time.deltaTime;
        }
    }
}
