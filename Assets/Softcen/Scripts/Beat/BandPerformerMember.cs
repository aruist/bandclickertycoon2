using UnityEngine;

[ExecuteAlways]
public class BandPerformerMember : MonoBehaviour
{
    public enum PerformerRole
    {
        Guitarist,
        Drummer
    }

    [SerializeField] private MeshRenderer meshRenderer;
    [Header("Identity")]
    [SerializeField] private PerformerRole role = PerformerRole.Guitarist;
    [SerializeField] private int performerIndex = 0;

    [Header("Rig")]
    [SerializeField] private Transform motionRoot;
    [SerializeField] private Transform secondaryMotion;

    [Header("Base Motion")]
    [SerializeField] private float grooveFrequency = 1.35f;
    [SerializeField] private float grooveSwayAngle = 2.4f;
    [SerializeField] private float grooveBobHeight = 0.012f;

    [Header("Beat Response")]
    [SerializeField] private float beatPulseDecay = 6.5f;
    [SerializeField] private float accentPulseDecay = 4.0f;
    [SerializeField] private float maxBeatLeanAngle = 8f;
    [SerializeField] private float maxBeatBobHeight = 0.05f;
    [SerializeField] private float maxScaleBoost = 0.05f;
    [SerializeField] private float maxSecondaryAngle = 14f;

    [Header("Role Weight")]
    [SerializeField] private float guitarPrimaryWeight = 1f;
    [SerializeField] private float drumPrimaryWeight = 1f;

    [Header("Atlas")]
    [SerializeField] private int atlasCols = 4;
    [SerializeField] private int atlasRows = 4;
    [SerializeField] private float frameChangeSpeed = 0.25f;
    [Range(0, 15)]
    [SerializeField] private int previewFrame = 0;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private Quaternion secondaryBaseRotation;
    [Header("Debug only")]
    [SerializeField] private float beatPulse;
    [SerializeField] private float accentPulse;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float rolePrimaryWeight = 1f;

    public PerformerRole Role => role;
    private MeshFilter meshFilter;
    private Vector3 meshBottomPivot;
    private MaterialPropertyBlock block;
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int AudienceAtlasST = Shader.PropertyToID("_AudienceAtlasST");
    private float frameTimer;

    private void Awake()
    {
        Init();
        if (!Application.isPlaying) return;

        if (motionRoot == null)
            motionRoot = transform;

        baseLocalPosition = motionRoot.localPosition;
        baseLocalRotation = motionRoot.localRotation;
        baseLocalScale = motionRoot.localScale;

        if (secondaryMotion != null)
            secondaryBaseRotation = secondaryMotion.localRotation;

        phaseOffset = performerIndex * 0.63f;
        rolePrimaryWeight = role == PerformerRole.Drummer ? drumPrimaryWeight : guitarPrimaryWeight;
    }

    [ContextMenu("Init")]
    private void Init()
    {
        frameTimer = 0;
        if (meshRenderer == null) return;

        if (meshFilter == null)
            meshFilter = meshRenderer.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            meshBottomPivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        if (block == null)
            block = new MaterialPropertyBlock();
        SetFrame(previewFrame);

    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameChangeSpeed)
        {
            frameTimer = 0;
            previewFrame++;
            if (previewFrame > 3) previewFrame = 0;
            SetFrame(previewFrame);
        }
        //if (Application.isPlaying) return;

        beatPulse = Mathf.MoveTowards(beatPulse, 0f, Time.deltaTime * beatPulseDecay);
        accentPulse = Mathf.MoveTowards(accentPulse, 0f, Time.deltaTime * accentPulseDecay);

        float t = (Time.time * grooveFrequency) + phaseOffset;
        float groove = Mathf.Sin(t);
        float pulseMix = Mathf.Clamp01(beatPulse + accentPulse * 0.65f) * rolePrimaryWeight;

        float bob = groove * grooveBobHeight + pulseMix * maxBeatBobHeight;
        float yaw = groove * grooveSwayAngle;
        float lean = pulseMix * maxBeatLeanAngle;

        if (role == PerformerRole.Drummer)
        {
            // Drummer sits more centered; stronger vertical pulse and less side sway.
            yaw *= 0.45f;
            bob *= 1.25f;
            lean *= 0.7f;
        }
        else
        {
            // Guitarists have larger side groove and strum-like twist.
            yaw *= 1.3f;
            lean *= 1.05f;
        }

        motionRoot.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        motionRoot.localRotation = baseLocalRotation * Quaternion.Euler(lean * 0.6f, yaw, -lean);
        motionRoot.localScale = baseLocalScale * (1f + pulseMix * maxScaleBoost);

        if (secondaryMotion != null)
        {
            float secondary = Mathf.Sin((t * 1.9f) + 1.2f) * maxSecondaryAngle * pulseMix;
            float x = role == PerformerRole.Drummer ? -secondary : secondary;
            secondaryMotion.localRotation = secondaryBaseRotation * Quaternion.Euler(x, 0f, 0f);
        }
    }

    public void ApplyBeat(BeatDetection.BeatType beatType, float intensity, bool isAccent, HypeState hypeState)
    {
        float normalizedIntensity = Mathf.Clamp01(intensity);
        float roleMatch = GetRoleBeatMatch(beatType);

        float hypeScale = 1f;
        frameChangeSpeed = 0.25f;
        switch (hypeState)
        {
            case HypeState.Low:
                frameChangeSpeed = 0.5f;
                hypeScale = 0.8f;
                break;
            case HypeState.High:
                frameChangeSpeed = 0.1f;
                hypeScale = 1.2f;
                break;
        }

        float pulse = normalizedIntensity * roleMatch * hypeScale;
        beatPulse = Mathf.Max(beatPulse, pulse);

        if (isAccent)
            accentPulse = Mathf.Max(accentPulse, pulse * 1.15f);
    }

    private float GetRoleBeatMatch(BeatDetection.BeatType beatType)
    {
        if (role == PerformerRole.Drummer)
        {
            if (beatType == BeatDetection.BeatType.Kick || beatType == BeatDetection.BeatType.BassDrum)
                return 1.2f;
            if (beatType == BeatDetection.BeatType.Snare || beatType == BeatDetection.BeatType.Cymbal)
                return 1.0f;
            if (beatType == BeatDetection.BeatType.Energy)
                return 1.05f;
            return 0.55f;
        }

        // Guitarist
        if (beatType == BeatDetection.BeatType.Snare || beatType == BeatDetection.BeatType.Cymbal || beatType == BeatDetection.BeatType.HiHat)
            return 1.15f;
        if (beatType == BeatDetection.BeatType.Kick || beatType == BeatDetection.BeatType.BassDrum)
            return 0.95f;
        if (beatType == BeatDetection.BeatType.Energy)
            return 1.0f;
        return 0.65f;
    }

    public void SetFrame(int frame)
    {
        frame = Mathf.Clamp(frame, 0, atlasCols * atlasRows - 1);
        previewFrame = frame;
        if (meshRenderer == null)
            return;

        meshRenderer.GetPropertyBlock(block);
        Vector4 atlasST = GetAtlasST(frame);
        block.SetVector(BaseMapST, atlasST);
        block.SetVector(AudienceAtlasST, atlasST);
        meshRenderer.SetPropertyBlock(block);
    }

    private Vector4 GetAtlasST(int frame)
    {
        int x = frame % atlasCols;
        int y = frame / atlasCols;

        float scaleX = 1f / atlasCols;
        float scaleY = 1f / atlasRows;
        float offsetX = x * scaleX;

        // Unity UV origin is bottom-left, but atlas frame order is top-left.
        float offsetY = 1f - scaleY - y * scaleY;

        return new Vector4(scaleX, scaleY, offsetX, offsetY);
    }

}
