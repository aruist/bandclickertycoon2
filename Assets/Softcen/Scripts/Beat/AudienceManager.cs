using System.Collections.Generic;
using UnityEngine;

public class AudienceManager : MonoBehaviour
{
    private const int MaxInstancesPerDraw = 1023;

    [SerializeField] private AudienceMember[] members;
    [SerializeField] private bool useInstancedRendering = true;

    [Header("Pose Groups")]
    [SerializeField] private int[] idleFrames = { 0, 5, 10, 14 };
    [SerializeField] private int[] cheerFrames = { 1, 3, 7, 9, 15 };
    [SerializeField] private int[] clapFrames = { 2 };
    [SerializeField] private int[] waveFrames = { 4, 6, 13, 15 };

    private readonly List<AudienceMember> tempMembers = new();
    private readonly Matrix4x4[] matrices = new Matrix4x4[MaxInstancesPerDraw];
    private readonly Vector4[] atlasSTs = new Vector4[MaxInstancesPerDraw];

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int AudienceAtlasST = Shader.PropertyToID("_AudienceAtlasST");

    private Mesh instancedMesh;
    private Material instancedMaterial;
    private MaterialPropertyBlock instancedBlock;

    private void Awake()
    {
        if (members == null || members.Length == 0)
            members = GetComponentsInChildren<AudienceMember>();

        ConfigureInstancedRendering();
    }

    private void LateUpdate()
    {
        DrawInstancedMembers();
    }

    private void OnEnable()
    {
        BeatPlay.OnBeatDetected += OnBeatDetected;
    }

    private void OnDisable()
    {
        BeatPlay.OnBeatDetected -= OnBeatDetected;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        switch (beatType)
        {
            case BeatDetection.BeatType.Kick:
                OnKickBeat(intensity);
                break;

            case BeatDetection.BeatType.Snare:
                OnSnareBeat(intensity);
                break;

            case BeatDetection.BeatType.HiHat:
                OnHighBeat(intensity);
                break;

            // case BeatDetection.BeatType.Drop:
            //     OnDropMoment(intensity);
            //     break;
        }
    }

    public void OnKickBeat(float strength)
    {
        PulseRandomMembers(0.75f, strength);
        ChangeRandomPoses(0.08f, cheerFrames);
    }

    public void OnSnareBeat(float strength)
    {
        PulseRandomMembers(0.35f, strength * 0.7f);
        ChangeRandomPoses(0.18f, clapFrames);
    }

    public void OnHighBeat(float strength)
    {
        ChangeRandomPoses(0.12f, waveFrames);
    }

    public void OnDropMoment()
    {
        for (int i = 0; i < members.Length; i++)
        {
            members[i].BeatPulse(1.5f);
            members[i].SetFrame(GetRandomFrame(cheerFrames));
        }
    }

    private void PulseRandomMembers(float chance, float strength)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (Random.value <= chance)
                members[i].BeatPulse(strength);
        }
    }

    private void ChangeRandomPoses(float chance, int[] frames)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (Random.value <= chance)
                members[i].SetFrame(GetRandomFrame(frames));
        }
    }

    private static int GetRandomFrame(int[] frames)
    {
        if (frames == null || frames.Length == 0)
            return 0;

        return frames[Random.Range(0, frames.Length)];
    }

    private void ConfigureInstancedRendering()
    {
        if (!Application.isPlaying || !useInstancedRendering || members == null || members.Length == 0)
            return;

        AudienceMember firstMember = members[0];
        instancedMesh = firstMember != null ? firstMember.Mesh : null;
        Material sourceMaterial = firstMember != null ? firstMember.SharedMaterial : null;
        Shader instancedShader = Shader.Find("Softcen/Audience Instanced Atlas");

        if (instancedMesh == null || sourceMaterial == null || instancedShader == null)
        {
            useInstancedRendering = false;
            return;
        }

        instancedMaterial = new Material(instancedShader)
        {
            name = $"{sourceMaterial.name} (Instanced Runtime)",
            renderQueue = sourceMaterial.renderQueue,
            enableInstancing = true
        };

        if (sourceMaterial.HasProperty(BaseMap))
            instancedMaterial.SetTexture(BaseMap, sourceMaterial.GetTexture(BaseMap));
        else if (sourceMaterial.HasProperty(MainTex))
            instancedMaterial.SetTexture(BaseMap, sourceMaterial.GetTexture(MainTex));

        if (sourceMaterial.HasProperty(BaseColor))
            instancedMaterial.SetColor(BaseColor, sourceMaterial.GetColor(BaseColor));
        else if (sourceMaterial.HasProperty(ColorId))
            instancedMaterial.SetColor(BaseColor, sourceMaterial.GetColor(ColorId));

        instancedBlock = new MaterialPropertyBlock();

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] != null)
                members[i].SetRuntimeRendererEnabled(false);
        }
    }

    private void DrawInstancedMembers()
    {
        if (!Application.isPlaying || !useInstancedRendering || instancedMesh == null || instancedMaterial == null)
            return;

        int count = 0;

        for (int i = 0; i < members.Length && count < MaxInstancesPerDraw; i++)
        {
            AudienceMember member = members[i];

            if (member == null || !member.gameObject.activeInHierarchy)
                continue;

            matrices[count] = member.transform.localToWorldMatrix;
            atlasSTs[count] = member.AtlasST;
            count++;
        }

        if (count == 0)
            return;

        instancedBlock.SetVectorArray(AudienceAtlasST, atlasSTs);
        Graphics.DrawMeshInstanced(instancedMesh, 0, instancedMaterial, matrices, count, instancedBlock);
    }
}
