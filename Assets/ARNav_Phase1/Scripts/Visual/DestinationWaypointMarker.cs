using UnityEngine;
using TMPro;

public class DestinationWaypointMarker : MonoBehaviour
{
    public static DestinationWaypointMarker I
    {
        get
        {
            if (_i == null) _i = Object.FindAnyObjectByType<DestinationWaypointMarker>(FindObjectsInactive.Include);
            return _i;
        }
        set { _i = value; }
    }
    private static DestinationWaypointMarker _i;

    [Header("Visual Components")]
    public Transform pinTransform;
    public Transform pulseRingTransform;
    public TMP_Text destinationLabel;

    [Header("Animation Settings")]
    public float bobSpeed = 2.5f;
    public float bobHeight = 0.15f;
    public float rotateSpeed = 60f;
    public float pulseSpeed = 1.2f;
    public float maxPulseScale = 1.6f;

    [Header("Activation Distance")]
    public float maxVisibleDistance = 15f;

    private Vector3 basePosition;
    private bool isActive = false;
    private MeshRenderer pinRenderer;
    private MeshRenderer ringRenderer;

    void Awake()
    {
        I = this;
        SetupProceduralVisuals();
        Hide();
    }

    void SetupProceduralVisuals()
    {
        // 1. Create Pin (Diamond / Gem mesh) if not assigned
        if (pinTransform == null)
        {
            var pinGo = new GameObject("MarkerPin");
            pinGo.transform.SetParent(transform, false);
            pinGo.transform.localPosition = new Vector3(0, 1.1f, 0); // Reduced by 0.7

            var mf = pinGo.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateDiamondMesh();

            pinRenderer = pinGo.AddComponent<MeshRenderer>();
            pinRenderer.sharedMaterial = CreateGlowMaterial(new Color(0f, 0.85f, 1f, 0.95f), true);
            pinTransform = pinGo.transform;
        }

        // 2. Create Ground Pulse Ring if not assigned
        if (pulseRingTransform == null)
        {
            var ringGo = new GameObject("GroundPulseRing");
            ringGo.transform.SetParent(transform, false);
            ringGo.transform.localPosition = new Vector3(0, 0.04f, 0); // Slightly above floor

            var mf = ringGo.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateFlatRingMesh(0.45f, 0.65f, 32);

            ringRenderer = ringGo.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = CreateGlowMaterial(new Color(0f, 0.85f, 1f, 0.6f), false);
            pulseRingTransform = ringGo.transform;
        }

        // 3. Create Floating Destination Label if not assigned
        if (destinationLabel == null)
        {
            var labelGo = new GameObject("MarkerLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0, 1.6f, 0);

            destinationLabel = labelGo.AddComponent<TextMeshPro>();
            destinationLabel.alignment = TextAlignmentOptions.Center;
            destinationLabel.fontSize = 3.5f;
            destinationLabel.color = Color.white;
            destinationLabel.fontStyle = FontStyles.Bold;

            labelGo.AddComponent<BillboardText>();
        }
    }

    public void ShowAt(Vector3 worldPos, string roomName)
    {
        if (I == null) I = this;
        if (pinTransform == null) SetupProceduralVisuals();
        basePosition = worldPos;
        transform.position = worldPos;
        if (destinationLabel != null)
        {
            destinationLabel.text = $"{roomName}";
        }
        isActive = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isActive) return;

        // Animate Pin (Bobbing and Rotating)
        if (pinTransform != null)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            pinTransform.localPosition = new Vector3(0, 1.1f + bobOffset, 0);
            pinTransform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        // Animate Ground Pulse Ring
        if (pulseRingTransform != null)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float currentScale = Mathf.Lerp(0.8f, maxPulseScale, pulse);
            pulseRingTransform.localScale = new Vector3(currentScale, 1f, currentScale);
        }

        // Distance-based fade / scale
        if (Camera.main != null)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, basePosition);
            if (dist > maxVisibleDistance)
            {
                transform.localScale = Vector3.zero;
            }
            else
            {
                float scale = Mathf.Clamp01((maxVisibleDistance - dist) / 3f);
                transform.localScale = Vector3.Lerp(Vector3.one * 0.4f, Vector3.one, scale);
            }
        }
    }

    // Procedural diamond / octahedron mesh
    static Mesh CreateDiamondMesh()
    {
        var m = new Mesh { name = "ProceduralDiamond" };
        float r = 0.22f;
        float hTop = 0.35f;
        float hBot = 0.45f;

        Vector3 top = new Vector3(0, hTop, 0);
        Vector3 bot = new Vector3(0, -hBot, 0);
        Vector3[] mid = new Vector3[4] {
            new Vector3( r, 0,  0),
            new Vector3( 0, 0,  r),
            new Vector3(-r, 0,  0),
            new Vector3( 0, 0, -r)
        };

        var vertices = new Vector3[] {
            // Top 4 faces
            top, mid[0], mid[1],
            top, mid[1], mid[2],
            top, mid[2], mid[3],
            top, mid[3], mid[0],
            // Bottom 4 faces
            bot, mid[1], mid[0],
            bot, mid[2], mid[1],
            bot, mid[3], mid[2],
            bot, mid[0], mid[3]
        };

        var triangles = new int[24];
        for (int i = 0; i < 24; i++) triangles[i] = i;

        m.vertices = vertices;
        m.triangles = triangles;
        m.RecalculateNormals();
        return m;
    }

    // Procedural flat ring mesh for floor pulse
    static Mesh CreateFlatRingMesh(float innerR, float outerR, int segments)
    {
        var m = new Mesh { name = "ProceduralRing" };
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float rad = (i / (float)segments) * Mathf.PI * 2f;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            vertices[i * 2] = new Vector3(cos * innerR, 0, sin * innerR);
            vertices[i * 2 + 1] = new Vector3(cos * outerR, 0, sin * outerR);

            int next = (i + 1) % segments;
            int t = i * 6;
            triangles[t + 0] = i * 2;
            triangles[t + 1] = i * 2 + 1;
            triangles[t + 2] = next * 2 + 1;

            triangles[t + 3] = i * 2;
            triangles[t + 4] = next * 2 + 1;
            triangles[t + 5] = next * 2;
        }

        m.vertices = vertices;
        m.triangles = triangles;
        m.RecalculateNormals();
        return m;
    }

    static Material CreateGlowMaterial(Color color, bool isDoubleSided)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        if (isDoubleSided) mat.SetInt("_Cull", 0);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }
}
