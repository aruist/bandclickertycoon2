using System.Collections.Generic;
using UnityEngine;

public class AudienceManager : MonoBehaviour
{
    private const int MaxInstancesPerDraw = 1023;

    private enum PoseGroup
    {
        Idle,
        Clap,
        Cheer,
        Wave
    }

    [SerializeField] private AudienceMember[] members;
    [SerializeField] private bool useInstancedRendering = true;
    [SerializeField] private GameObject[] audiencePrefabs;
    [SerializeField] private int audienceSize;
    [SerializeField] private Transform audienceFloor;

    [Header("Pose Families")]
    [Tooltip("For a 4x4 atlas where rows are pose groups and columns are personality variants.")]
    [SerializeField] private bool useAtlasRowFamilies = true;
    [SerializeField] private int atlasColumns = 4;
    [SerializeField] private int idleRowStartFrame = 0;
    [SerializeField] private int clapRowStartFrame = 4;
    [SerializeField] private int cheerRowStartFrame = 8;
    [SerializeField] private int waveRowStartFrame = 12;
    [SerializeField] private float minPoseHoldTime = 0.35f;
    [SerializeField] private float maxPoseHoldTime = 0.8f;

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
    private int[] poseFamilyIndices;
    private float[] nextPoseChangeTimes;

    private void Awake()
    {
        SpawnAudienceFromPrefabs();

        if (members == null || members.Length == 0)
            members = audienceFloor.GetComponentsInChildren<AudienceMember>();

        ConfigurePoseFamilies();
        ConfigureInstancedRendering();
    }

    private void SpawnAudienceFromPrefabs()
    {
        if (!Application.isPlaying)
            return;

        if (audiencePrefabs == null || audiencePrefabs.Length == 0 || audienceSize <= 0 || audienceFloor == null)
            return;

        Bounds localBounds = GetAudienceFloorLocalBounds();
        float floorY = localBounds.center.y;
        float floorYaw = audienceFloor.rotation.eulerAngles.y;

        for (int i = 0; i < audienceSize; i++)
        {
            GameObject prefab = audiencePrefabs[Random.Range(0, audiencePrefabs.Length)];
            if (prefab == null)
                continue;

            float localX = Random.Range(localBounds.min.x, localBounds.max.x);
            float localZ = Random.Range(localBounds.min.z, localBounds.max.z);
            Vector3 localPosition = new Vector3(localX, floorY, localZ);
            Quaternion spawnRotation = Quaternion.Euler(0f, floorYaw, 0f);

            Instantiate(prefab, audienceFloor.TransformPoint(localPosition), spawnRotation, audienceFloor);
        }
    }

    private Bounds GetAudienceFloorLocalBounds()
    {
        MeshFilter meshFilter = audienceFloor.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
            return meshFilter.sharedMesh.bounds;

        Collider floorCollider = audienceFloor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            Vector3 centerLocal = audienceFloor.InverseTransformPoint(floorCollider.bounds.center);
            Vector3 extentsWorld = floorCollider.bounds.extents;
            Vector3 extentsLocal = new Vector3(
                Mathf.Abs(extentsWorld.x / Mathf.Max(0.0001f, audienceFloor.lossyScale.x)),
                Mathf.Abs(extentsWorld.y / Mathf.Max(0.0001f, audienceFloor.lossyScale.y)),
                Mathf.Abs(extentsWorld.z / Mathf.Max(0.0001f, audienceFloor.lossyScale.z)));

            return new Bounds(centerLocal, extentsLocal * 2f);
        }

        return new Bounds(Vector3.zero, Vector3.one);
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
            //     OnDropMoment();
            //     break;
        }
    }

    public void OnKickBeat(float strength)
    {
        PulseRandomMembers(0.75f, strength);
        ChangeRandomPoses(0.08f, PoseGroup.Cheer);
    }

    public void OnSnareBeat(float strength)
    {
        PulseRandomMembers(0.35f, strength * 0.7f);
        ChangeRandomPoses(0.18f, PoseGroup.Clap);
    }

    public void OnHighBeat(float strength)
    {
        ChangeRandomPoses(0.12f, PoseGroup.Wave);
    }

    public void OnDropMoment(float strength)
    {
        for (int i = 0; i < members.Length; i++)
        {
            members[i].BeatPulse(1.5f);
            TrySetPose(i, PoseGroup.Cheer, true);
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

    private void ChangeRandomPoses(float chance, PoseGroup poseGroup)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (Random.value <= chance)
                TrySetPose(i, poseGroup, false);
        }
    }

    private void ConfigurePoseFamilies()
    {
        if (members == null)
            return;

        poseFamilyIndices = new int[members.Length];
        nextPoseChangeTimes = new float[members.Length];
        int familyCount = Mathf.Max(1, atlasColumns);

        for (int i = 0; i < members.Length; i++)
        {
            poseFamilyIndices[i] = i % familyCount;
            nextPoseChangeTimes[i] = 0f;

            if (members[i] != null)
                members[i].SetFrame(GetFrameForMember(members[i].AtlasColumn, PoseGroup.Idle));
        }
    }

    private void TrySetPose(int memberIndex, PoseGroup poseGroup, bool ignoreCooldown)
    {
        if (members == null || memberIndex < 0 || memberIndex >= members.Length || members[memberIndex] == null)
            return;

        if (!ignoreCooldown && Time.time < nextPoseChangeTimes[memberIndex])
            return;

        int atlasColumn = members[memberIndex].AtlasColumn;
        members[memberIndex].SetFrame(GetFrameForMember(atlasColumn, poseGroup));
        float minHold = Mathf.Min(minPoseHoldTime, maxPoseHoldTime);
        float maxHold = Mathf.Max(minPoseHoldTime, maxPoseHoldTime);
        nextPoseChangeTimes[memberIndex] = Time.time + Random.Range(minHold, maxHold);
    }

    private int GetFrameForMember(int memberIndex, PoseGroup poseGroup)
    {
        if (memberIndex < 0 || memberIndex > 3) return 0;
        switch (poseGroup)
        {
            case PoseGroup.Clap:
                return clapFrames[memberIndex];
            case PoseGroup.Cheer:
                return cheerFrames[memberIndex];
            case PoseGroup.Wave:
                return waveFrames[memberIndex];
            default:
                return idleFrames[memberIndex];
        }

        // if (useAtlasRowFamilies)
        // {
        //     int familyIndex = poseFamilyIndices != null && memberIndex < poseFamilyIndices.Length
        //         ? poseFamilyIndices[memberIndex]
        //         : memberIndex;

        //     int rowStart = GetRowStartFrame(poseGroup);
        //     return rowStart + Mathf.Abs(familyIndex % Mathf.Max(1, atlasColumns));
        // }

        // return GetRandomFrame(GetLegacyFrames(poseGroup));
    }

    private int GetRowStartFrame(PoseGroup poseGroup)
    {
        switch (poseGroup)
        {
            case PoseGroup.Clap:
                return clapRowStartFrame;
            case PoseGroup.Cheer:
                return cheerRowStartFrame;
            case PoseGroup.Wave:
                return waveRowStartFrame;
            default:
                return idleRowStartFrame;
        }
    }

    private int[] GetLegacyFrames(PoseGroup poseGroup)
    {
        switch (poseGroup)
        {
            case PoseGroup.Clap:
                return clapFrames;
            case PoseGroup.Cheer:
                return cheerFrames;
            case PoseGroup.Wave:
                return waveFrames;
            default:
                return idleFrames;
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
