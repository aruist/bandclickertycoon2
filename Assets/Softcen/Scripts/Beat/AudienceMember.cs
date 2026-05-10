using UnityEngine;

[ExecuteAlways]
public class AudienceMember : MonoBehaviour
{
    public enum MotionStyle
    {
        IdleSway,
        HeadBob,
        Clap,
        Wave,
        Jump
    }

    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Transform swayTransform;
    [SerializeField] private float swayRotation = 20f;
    [SerializeField] private float swayX = 1f;
    [SerializeField] private int atlasColumn = 0;

    [Header("Motion")]
    [SerializeField] private float idleSwaySpeed = 1.2f;
    [SerializeField] private float activeSwaySpeed = 2.2f;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float jumpHeight = 0.22f;
    [SerializeField] private float pulseScaleBoost = 0.08f;
    [SerializeField] private float styleHoldTime = 0.35f;

    [Header("Atlas")]
    [SerializeField] private int atlasCols = 4;
    [SerializeField] private int atlasRows = 4;

    [Range(0, 15)]
    [SerializeField] private int previewFrame = 0;

    private MaterialPropertyBlock block;
    private MeshFilter meshFilter;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private float pulse;
    private float targetPulse;
    private float jumpPulse;
    private float targetJumpPulse;
    private float currentSwaySpeed;
    private float styleTimer;
    private float swayOffset;
    private float randomPower = 1f;
    private MotionStyle motionStyle = MotionStyle.IdleSway;

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int AudienceAtlasST = Shader.PropertyToID("_AudienceAtlasST");

    public int AtlasColumn => atlasColumn;

    private void Awake()
    {
        Init();

        if (Application.isPlaying)
        {
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            baseRotation = swayTransform.localRotation;

            swayOffset = Random.value * 100f;
            randomPower = Random.Range(0.75f, 1.25f);
            currentSwaySpeed = idleSwaySpeed;

            SetFrame(previewFrame);
        }
    }

    private void OnEnable()
    {
        Init();
        SetFrame(previewFrame);
    }

    private void OnValidate()
    {
        Init();
        SetFrame(previewFrame);
    }

    private void Init()
    {
        if (swayTransform == null) swayTransform = transform;

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (block == null)
            block = new MaterialPropertyBlock();
    }

    public Mesh Mesh
    {
        get
        {
            Init();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }
    }

    public Material SharedMaterial
    {
        get
        {
            Init();
            return meshRenderer != null ? meshRenderer.sharedMaterial : null;
        }
    }

    public int PreviewFrame => previewFrame;

    public Vector4 AtlasST => GetAtlasST(previewFrame);

    public void SetRuntimeRendererEnabled(bool enabled)
    {
        Init();

        if (meshRenderer != null)
            meshRenderer.enabled = enabled;
    }

    public void BeatPulse(float strength)
    {
        TriggerReaction(MotionStyle.HeadBob, strength);
    }

    public void TriggerReaction(MotionStyle style, float strength)
    {
        if (!Application.isPlaying)
            return;

        float weightedStrength = Mathf.Clamp01(strength * randomPower);
        targetPulse = Mathf.Max(targetPulse, weightedStrength);
        motionStyle = style;
        styleTimer = styleHoldTime;

        if (style == MotionStyle.Jump)
            targetJumpPulse = Mathf.Max(targetJumpPulse, weightedStrength);
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

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        targetPulse = Mathf.MoveTowards(targetPulse, 0f, Time.deltaTime * 2.5f);
        pulse = Mathf.Lerp(pulse, targetPulse, Time.deltaTime * 12f);
        targetJumpPulse = Mathf.MoveTowards(targetJumpPulse, 0f, Time.deltaTime * 4.5f);
        jumpPulse = Mathf.Lerp(jumpPulse, targetJumpPulse, Time.deltaTime * 14f);

        if (styleTimer > 0f)
            styleTimer -= Time.deltaTime;
        else
            motionStyle = MotionStyle.IdleSway;

        float targetSwaySpeed = motionStyle == MotionStyle.IdleSway ? idleSwaySpeed : activeSwaySpeed;
        currentSwaySpeed = Mathf.Lerp(currentSwaySpeed, targetSwaySpeed, Time.deltaTime * 4f);

        float sway = Mathf.Sin(Time.time * currentSwaySpeed + swayOffset) * 0.025f;
        float swayXcoord = sway * swayX;
        float height = pulse * bobHeight + jumpPulse * jumpHeight;

        transform.localPosition = basePosition + new Vector3(swayXcoord, height, 0f);
        transform.localScale = baseScale * (1f + pulse * pulseScaleBoost);
        swayTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, sway * swayRotation);
    }
}
