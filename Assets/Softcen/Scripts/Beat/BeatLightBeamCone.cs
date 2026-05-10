using UnityEngine;

/// <summary>
/// Pulses a fake light-beam cone from BeatPlay beat events.
/// Attach this to the cone mesh object that uses a transparent/additive material.
///
/// Recommended material:
/// - Unlit transparent/additive shader
/// - Soft white vertical gradient texture
/// - GPU instancing enabled if many beams use the same material
///
/// This script uses MaterialPropertyBlock, so it does not create material instances at runtime.
/// </summary>
[DisallowMultipleComponent]
public sealed class BeatLightBeamCone : MonoBehaviour
{
    [SerializeField] private StageLightGroup stageLightGroup;
    [SerializeField] private int stageLightGroupIndex;
    [Header("Beat Source")]
    [Tooltip("BeatPlay instance that emits beat events. If empty, the script can listen to BeatPlay.OnBeatDetected globally.")]
    [SerializeField] private BeatPlay beatPlay;

    [Tooltip("Use the global BeatPlay.OnBeatDetected event instead of a specific BeatPlay instance. Useful for simple scenes, but avoid it if you have multiple songs/BeatPlay objects.")]
    [SerializeField] private bool useGlobalBeatEvent = false;

    [Tooltip("Which beat types should affect this beam.")]
    [SerializeField] private BeatDetection.BeatType reactToBeats =
        BeatDetection.BeatType.Kick |
        BeatDetection.BeatType.BassDrum |
        BeatDetection.BeatType.Energy;

    [Header("Renderer")]
    [SerializeField] private Renderer beamRenderer;

    [Tooltip("Usually _BaseColor in URP Lit/Unlit, _Color in Built-in shaders.")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Alpha")]
    [Range(0f, 1f)]
    [SerializeField] private float idleAlpha = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float maxPulseAlpha = 0.85f;

    [Tooltip("Multiplier for beat intensity coming from JSON.")]
    [SerializeField, Min(0f)] private float intensityMultiplier = 1.0f;

    [Header("Pulse Feel")]
    [Tooltip("How fast the beam jumps up after a beat. Higher = snappier.")]
    [SerializeField, Min(0.01f)] private float attackSpeed = 30f;

    [Tooltip("How fast the beam fades after a beat. Higher = shorter pulse.")]
    [SerializeField, Min(0.01f)] private float releaseSpeed = 6f;

    [Tooltip("Extra pulse smoothing. 1 = linear, 2 = punchier, 3 = very punchy.")]
    [SerializeField, Min(0.25f)] private float pulseCurvePower = 1.6f;

    [Header("Scale Pulse")]
    [SerializeField] private bool pulseScale = true;

    [Tooltip("Local X/Z scale boost at full pulse. Y is usually cone length, so it is not changed by default.")]
    [SerializeField, Min(0f)] private float widthScaleBoost = 0.25f;

    [Tooltip("Optional length scale boost at full pulse. Keep small for light cones.")]
    [SerializeField, Min(0f)] private float lengthScaleBoost = 0.0f;

    [Header("Optional Point Light")]
    [SerializeField] private Light pointLight;
    [SerializeField] private bool pulsePointLightIntensity = false;
    [SerializeField, Min(0f)] private float minPointLightIntensity = 0f;
    [SerializeField, Min(0f)] private float maxPointLightIntensity = 6f;

    [Header("Motion")]
    [Tooltip("Optional small sweep movement. Good for stage lights.")]
    [SerializeField] private bool enableSweep = false;

    [SerializeField] private Vector3 sweepAxis = Vector3.up;
    [SerializeField] private float sweepDegrees = 12f;
    [SerializeField] private float sweepSpeed = 1.5f;
    [SerializeField] private Transform sweepTransform;

    [Header("Randomness")]
    [Tooltip("Adds small variation to avoid many lights pulsing exactly the same way.")]
    [SerializeField, Range(0f, 0.5f)] private float randomIntensityVariation = 0.08f;

    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyId;
    private float pulse;
    private float targetPulse;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private bool subscribed;
    private bool started;
    private Color baseColor;
    private Color pulseColor;

    private void Reset()
    {
        beamRenderer = GetComponent<Renderer>();
        beatPlay = FindFirstObjectByType<BeatPlay>();
        if (pointLight == null)
            pointLight = GetComponentInChildren<Light>();
    }

    private void Awake()
    {
        started = false;
        if (beamRenderer == null)
            beamRenderer = GetComponent<Renderer>();

        propertyBlock = new MaterialPropertyBlock();
        colorPropertyId = Shader.PropertyToID(colorPropertyName);
        baseLocalScale = transform.localScale;
        if (sweepTransform != null) baseLocalRotation = sweepTransform.localRotation;
        else baseLocalRotation = transform.localRotation;

    }

    void Start()
    {
        ApplyColors();
        started = true;
        ApplyVisuals(0f);
    }

    private void ApplyColors()
    {
        if (BeatStageColorSync.Instance == null)
        {
            baseColor = Color.white;
            pulseColor = Color.white;
            return;
        }
        baseColor = BeatStageColorSync.Instance.GetColor(stageLightGroup, stageLightGroupIndex);
        pulseColor = BeatStageColorSync.Instance.GetPulseColor(stageLightGroup, stageLightGroupIndex);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        float speed = targetPulse > pulse ? attackSpeed : releaseSpeed;
        pulse = Mathf.MoveTowards(pulse, targetPulse, speed * Time.deltaTime);
        targetPulse = Mathf.MoveTowards(targetPulse, 0f, releaseSpeed * Time.deltaTime);

        float shapedPulse = Mathf.Pow(Mathf.Clamp01(pulse), pulseCurvePower);
        ApplyVisuals(shapedPulse);
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (useGlobalBeatEvent)
        {
            BeatPlay.OnBeatDetected += OnBeatDetected;
            subscribed = true;
            return;
        }

        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (beatPlay != null)
        {
            beatPlay.BeatDetected += OnBeatDetected;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (useGlobalBeatEvent)
        {
            BeatPlay.OnBeatDetected -= OnBeatDetected;
        }
        else if (beatPlay != null)
        {
            beatPlay.BeatDetected -= OnBeatDetected;
        }

        subscribed = false;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        if (!started || (reactToBeats & beatType) == 0)
            return;

        float variation = 1f;
        if (randomIntensityVariation > 0f)
            variation += Random.Range(-randomIntensityVariation, randomIntensityVariation);

        float newPulse = Mathf.Clamp01(intensity * intensityMultiplier * variation);

        // Keep the strongest beat if multiple beat events arrive in the same frame.
        targetPulse = Mathf.Max(targetPulse, newPulse);
    }

    private void ApplyVisuals(float shapedPulse)
    {
        if (beamRenderer != null)
        {
            float alpha = Mathf.Lerp(idleAlpha, maxPulseAlpha, shapedPulse);
            Color color = Color.Lerp(baseColor, pulseColor, shapedPulse);
            color.a = alpha;

            beamRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, color);
            beamRenderer.SetPropertyBlock(propertyBlock);
        }

        if (pulseScale)
        {
            float widthScale = 1f + shapedPulse * widthScaleBoost;
            float lengthScale = 1f + shapedPulse * lengthScaleBoost;
            transform.localScale = new Vector3(
                baseLocalScale.x * widthScale,
                baseLocalScale.y * lengthScale,
                baseLocalScale.z * widthScale
            );
        }

        if (pulsePointLightIntensity && pointLight != null)
        {
            float minIntensity = Mathf.Min(minPointLightIntensity, maxPointLightIntensity);
            float maxIntensity = Mathf.Max(minPointLightIntensity, maxPointLightIntensity);
            pointLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, shapedPulse);
        }

        if (enableSweep)
        {
            float angle = Mathf.Sin(Time.time * sweepSpeed) * sweepDegrees;
            if (sweepTransform != null)
            {
                sweepTransform.localRotation = baseLocalRotation * Quaternion.AngleAxis(angle, sweepAxis.normalized);
            }
            else
            {
                transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(angle, sweepAxis.normalized);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        idleAlpha = Mathf.Clamp01(idleAlpha);
        maxPulseAlpha = Mathf.Clamp01(maxPulseAlpha);
        attackSpeed = Mathf.Max(0.01f, attackSpeed);
        releaseSpeed = Mathf.Max(0.01f, releaseSpeed);
        pulseCurvePower = Mathf.Max(0.25f, pulseCurvePower);
        intensityMultiplier = Mathf.Max(0f, intensityMultiplier);
        minPointLightIntensity = Mathf.Max(0f, minPointLightIntensity);
        maxPointLightIntensity = Mathf.Max(0f, maxPointLightIntensity);

        if (beamRenderer == null)
            beamRenderer = GetComponent<Renderer>();
    }
#endif
}
