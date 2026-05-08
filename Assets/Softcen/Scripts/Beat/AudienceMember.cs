using UnityEngine;

[ExecuteAlways]
public class AudienceMember : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Atlas")]
    [SerializeField] private int atlasCols = 4;
    [SerializeField] private int atlasRows = 4;

    [Range(0, 15)]
    [SerializeField] private int previewFrame = 0;

    private MaterialPropertyBlock block;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private float pulse;
    private float targetPulse;
    private float swayOffset;
    private float randomPower = 1f;

    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

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

        if (block == null)
            block = new MaterialPropertyBlock();
    }

    public void BeatPulse(float strength)
    {
        if (!Application.isPlaying)
            return;

        targetPulse = Mathf.Max(targetPulse, strength * randomPower);
    }

    public void SetFrame(int frame)
    {
        if (meshRenderer == null)
            return;

        frame = Mathf.Clamp(frame, 0, atlasCols * atlasRows - 1);
        previewFrame = frame;

        int x = frame % atlasCols;
        int y = frame / atlasCols;

        float scaleX = 1f / atlasCols;
        float scaleY = 1f / atlasRows;

        float offsetX = x * scaleX;

        // Unity UV origin is bottom-left, but atlas frame order is top-left.
        float offsetY = 1f - scaleY - y * scaleY;

        meshRenderer.GetPropertyBlock(block);
        block.SetVector(BaseMapST, new Vector4(scaleX, scaleY, offsetX, offsetY));
        meshRenderer.SetPropertyBlock(block);
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