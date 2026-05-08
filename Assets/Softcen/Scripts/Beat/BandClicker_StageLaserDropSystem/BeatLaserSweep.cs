using UnityEngine;
using static BeatDetection;

/// <summary>
/// Beat-reactive laser that can render either with a LineRenderer or with a Quad/MeshRenderer.
///
/// Quad setup recommendation:
/// - Create GameObject > 3D Object > Quad
/// - Local X = width, local Y = length, pivot center
/// - Use an Unlit Transparent/Additive material
/// - Assign the Quad Renderer to quadRenderer, or place this script on the Quad object
///
/// LineRenderer setup:
/// - Add/keep LineRenderer on the same GameObject
/// - Use an Unlit Transparent/Additive material
/// </summary>
public class BeatLaserSweep : MonoBehaviour
{
    public enum LaserRenderMode
    {
        LineRenderer,
        QuadMesh
    }

    [Header("Render Mode")]
    [SerializeField] private LaserRenderMode renderMode = LaserRenderMode.LineRenderer;

    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Renderer quadRenderer;
    [SerializeField] private Camera facingCamera;

    [Header("Color Sync")]
    [SerializeField] private bool useSongColorSync = true;
    [SerializeField] private int colorIndex;
    [SerializeField] private Color fallbackColor = Color.cyan;
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Beat Filter")]
    [SerializeField] private BeatDetection.BeatType reactToBeats =
        BeatDetection.BeatType.HiHat |
        BeatDetection.BeatType.Cymbal |
        BeatDetection.BeatType.High;

    [Header("Laser Visibility")]
    [SerializeField] private float baseAlpha = 0.0f;
    [SerializeField] private float maxAlpha = 0.9f;
    [SerializeField] private float baseWidth = 0.005f;
    [SerializeField] private float maxWidth = 0.06f;
    [SerializeField] private float intensityMultiplier = 1.5f;
    [SerializeField] private float releaseSpeed = 22f;
    [SerializeField] private float visibleAlphaThreshold = 0.005f;

    [Header("Quad Mesh Settings")]
    [Tooltip("For a Unity Quad, keep this ON. Local X = width, local Y = length.")]
    [SerializeField] private bool scaleQuadToBeamLength = true;

    [Tooltip("Makes the quad face the camera while its local Y follows the laser direction.")]
    [SerializeField] private bool billboardQuadToCamera = true;

    [Tooltip("Small offset to prevent z-fighting if the quad is close to stage surfaces.")]
    [SerializeField] private float quadForwardOffset = 0.0f;

    [Header("Sweep Motion")]
    [SerializeField] private bool useSweep = true;
    [SerializeField] private float sweepAngle = 45f;
    [SerializeField] private float sweepSpeed = 1.4f;
    [SerializeField] private float verticalAngle = 0f;
    [SerializeField] private float beatJitterAngle = 4f;
    [SerializeField] private float jitterReturnSpeed = 8f;

    [Header("Fallback Beam")]
    [SerializeField] private float fallbackLength = 10f;

    private MaterialPropertyBlock mpb;
    private int colorPropertyId;
    private static readonly int BuiltInColorId = Shader.PropertyToID("_Color");

    private Quaternion baseRotation;
    private float pulse;
    private float jitter;
    private float randomPhase;
    private Vector3 initialQuadScale;
    private Vector3 initialPosition;

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
        quadRenderer = GetComponent<Renderer>();
        facingCamera = Camera.main;
    }

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (quadRenderer == null)
            quadRenderer = GetComponent<Renderer>();

        if (facingCamera == null)
            facingCamera = Camera.main;

        mpb = new MaterialPropertyBlock();
        colorPropertyId = Shader.PropertyToID(colorPropertyName);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        if (quadRenderer != null)
        {
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            initialQuadScale = quadRenderer.transform.localScale;
            initialPosition = transform.localPosition;
        }

        baseRotation = transform.rotation;
        randomPhase = Random.value * 100f;
    }

    private void Start()
    {
        ApplyColor(GetCurrentColor(), baseAlpha);
    }

    private void OnEnable()
    {
        BeatPlay.OnBeatDetected += OnBeatDetected;
    }

    private void OnDisable()
    {
        BeatPlay.OnBeatDetected -= OnBeatDetected;
    }

    private void Update()
    {
        UpdateSweepRotation();

        Vector3 start = transform.position;
        Vector3 end = GetBeamEnd(start);

        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * releaseSpeed);

        float width = Mathf.Lerp(baseWidth, maxWidth, pulse);
        float alpha = Mathf.Lerp(baseAlpha, maxAlpha, pulse);
        bool visible = alpha > visibleAlphaThreshold;

        if (renderMode == LaserRenderMode.LineRenderer)
            UpdateLineRenderer(start, end, width, visible);
        else
        {
            start = initialPosition;
            end = GetBeamEnd(start);
            UpdateQuadMesh(start, end, width, visible);
        }

        ApplyColor(GetCurrentColor(), alpha);
    }

    private void UpdateSweepRotation()
    {
        if (!useSweep)
            return;

        float sweep = Mathf.Sin((Time.time + randomPhase) * sweepSpeed) * sweepAngle;
        jitter = Mathf.MoveTowards(jitter, 0f, Time.deltaTime * jitterReturnSpeed);

        transform.rotation =
            baseRotation *
            Quaternion.Euler(verticalAngle, sweep + jitter, 0f);
    }

    private Vector3 GetBeamEnd(Vector3 start)
    {
        if (target != null)
            return target.position;

        return start + transform.forward * fallbackLength;
    }

    private void UpdateLineRenderer(Vector3 start, Vector3 end, float width, bool visible)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.enabled = visible;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        if (quadRenderer != null && quadRenderer != lineRenderer.GetComponent<Renderer>())
            quadRenderer.enabled = false;
    }

    private void UpdateQuadMesh(Vector3 start, Vector3 end, float width, bool visible)
    {
        if (quadRenderer == null)
            return;

        if (lineRenderer != null)
            lineRenderer.enabled = false;

        quadRenderer.enabled = visible;

        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length <= 0.0001f)
            return;

        direction /= length;

        Transform quadTransform = quadRenderer.transform;
        quadTransform.position = (start + end) * 0.5f;

        if (billboardQuadToCamera)
        {
            Vector3 forward;

            if (facingCamera != null)
                forward = -facingCamera.transform.forward;
            else
                forward = Vector3.forward;

            // Avoid invalid rotation if camera forward is almost parallel with beam direction.
            if (Mathf.Abs(Vector3.Dot(forward.normalized, direction)) > 0.98f)
                forward = Vector3.Cross(direction, Vector3.right).sqrMagnitude > 0.001f
                    ? Vector3.Cross(direction, Vector3.right)
                    : Vector3.Cross(direction, Vector3.up);

            quadTransform.rotation = Quaternion.LookRotation(forward, direction);
        }
        else
        {
            quadTransform.rotation = Quaternion.LookRotation(transform.forward, direction);
        }

        if (quadForwardOffset != 0f)
            quadTransform.position += quadTransform.forward * quadForwardOffset;

        if (scaleQuadToBeamLength)
            quadTransform.localScale = new Vector3(width, length, initialQuadScale.z == 0f ? 1f : initialQuadScale.z);
    }

    private Color GetCurrentColor()
    {
        if (useSongColorSync && BeatStageColorSync.Instance != null)
            return BeatStageColorSync.Instance.GetColor(StageLightGroup.Laser, colorIndex);

        return fallbackColor;
    }

    private void ApplyColor(Color color, float alpha)
    {
        color.a = alpha;

        if (renderMode == LaserRenderMode.LineRenderer && lineRenderer != null)
        {
            Color endColor = color;
            endColor.a = 0f;
            lineRenderer.startColor = color;
            lineRenderer.endColor = endColor;
        }

        Renderer activeRenderer = renderMode == LaserRenderMode.LineRenderer
            ? (lineRenderer != null ? lineRenderer.GetComponent<Renderer>() : null)
            : quadRenderer;

        if (activeRenderer == null)
            return;

        activeRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(colorPropertyId, color);
        mpb.SetColor(BuiltInColorId, color);
        activeRenderer.SetPropertyBlock(mpb);
    }

    private void OnBeatDetected(BeatType beatType, float intensity)
    {
        if ((reactToBeats & beatType) == 0)
            return;

        float p = Mathf.Clamp01(intensity * intensityMultiplier);
        pulse = Mathf.Max(pulse, p);

        if (beatJitterAngle > 0f)
            jitter += Random.Range(-beatJitterAngle, beatJitterAngle);
    }
}
