using UnityEngine;

[ExecuteAlways]
public class AudienceMember : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private int atlasColumn = 0;

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
    private float swayOffset;
    private float randomPower = 1f;

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
            baseRotation = transform.localRotation;

            swayOffset = Random.value * 100f;
            randomPower = Random.Range(0.75f, 1.25f);

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
        if (!Application.isPlaying)
            return;

        targetPulse = Mathf.Max(targetPulse, strength * randomPower);
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

        float sway = Mathf.Sin(Time.time * 2.2f + swayOffset) * 0.025f;

        transform.localPosition = basePosition + new Vector3(sway, pulse * 0.08f, 0f);
        transform.localScale = baseScale * (1f + pulse * 0.08f);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, sway * 20f);
    }
}
