using System.Collections.Generic;
using UnityEngine;
using WhiteCat;

/*
The AudienceManager.cs script effectively bridges the gap between the high-precision beat data from PCMBeatDetection
and the visual representation of the audience. It utilizes an instanced rendering approach to handle large audience
sizes efficiently while providing rhythmic behaviors based on song "Hype" levels.
*/
public class AudienceManager : MonoBehaviour
{
    public static AudienceManager Instance;
    private const int MaxInstancesPerDraw = 1023;

    public enum PoseGroup
    {
        Idle,
        Clap,
        Cheer,
        Wave
    }

    [SerializeField] private BeatPlay beatPlay;
    [SerializeField] private bool useGlobalBeatFallback = true;
    [SerializeField] private AudienceMember[] members;
    [SerializeField] private bool useInstancedRendering = true;
    [SerializeField] private GameObject[] audiencePrefabs;
    [Max(MaxInstancesPerDraw)]
    [SerializeField] private int audienceSize;
    [SerializeField] private Transform audienceFloor;

    [Header("Pose Timing")]
    [Tooltip("For a 4x4 atlas where rows are pose groups and columns are personality variants.")]
    [SerializeField] private float minPoseHoldTime = 0.35f;
    [SerializeField] private float maxPoseHoldTime = 0.8f;

    [Header("Pose Groups")]
    [SerializeField] private int[] idleFrames = { 0, 5, 10, 14 };
    [SerializeField] private int[] cheerFrames = { 1, 3, 7, 9, 15 };
    [SerializeField] private int[] clapFrames = { 2 };
    [SerializeField] private int[] waveFrames = { 4, 6, 13, 15 };

    [Header("Audience Rhythm")]
    [SerializeField] private float lookAheadSeconds = 10f;
    [SerializeField] private int quietWindowBeatThreshold = 8;
    [SerializeField] private float mediumBeatsPerSecond = 1.2f;
    [SerializeField] private float highAverageIntensity = 0.55f;
    [SerializeField] private int highUniqueBeatTypes = 5;
    [SerializeField] private float simultaneousBeatWindow = 0.08f;
    [SerializeField] private int moshUniqueBeatTypes = 5;

    [Header("Audience Delay")]
    [SerializeField] private float randomLatencyMax = 0.07f;
    [SerializeField] private float backRowDelay = 0.1f;
    [SerializeField] private float audienceTimingOffset = 0f;
    [SerializeField] private bool frontIsLowerLocalZ = true;

    [Header("Shader")]
    [SerializeField] private bool useLit = false;
    [SerializeField] private int audienceRenderQueueOffset = -50;
    [SerializeField] private bool logRenderPathOnDevice = true;

    private readonly Matrix4x4[] matrices = new Matrix4x4[MaxInstancesPerDraw];
    private readonly Vector4[] atlasSTs = new Vector4[MaxInstancesPerDraw];
    private readonly List<AudienceDrawEntry> drawEntries = new List<AudienceDrawEntry>(MaxInstancesPerDraw);
    private AudienceMember[] sortedDrawMembers;

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int AudienceAtlasST = Shader.PropertyToID("_AudienceAtlasST");
    private const string InstancedUnlitShaderName = "Softcen/Audience Instanced Atlas";
    private const string InstancedLitShaderName = "Softcen/Audience Instanced Atlas Lit";

    public Mesh instancedMesh;
    public Material instancedMaterial;
    private Material fallbackRuntimeMaterial;
    private MaterialPropertyBlock instancedBlock;
    private float[] nextPoseChangeTimes;
    private float[] cachedSpatialDelays;
    private Bounds audienceLocalBounds;
    private bool subscribedToBeatPlay;
    private bool subscribedToGlobalBeat;
    private HypeState currentHypeState = HypeState.Low;
    private bool currentMoshZone;
    private int hypeEventIndex;
    private float previousBeatTimestamp;
    private bool hypeEventsSorted;
    private readonly List<GameObject> spawnedAudienceInstances = new List<GameObject>();
    public float CurrentAudienceSongTime => beatPlay != null ? beatPlay.CurrentSongTime : Time.time;

    private struct AudienceDrawEntry
    {
        public AudienceMember Member;
        public float Depth;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SpawnAudienceFromPrefabs();

        if (members == null || members.Length == 0)
            members = audienceFloor != null
                ? audienceFloor.GetComponentsInChildren<AudienceMember>()
                : GetComponentsInChildren<AudienceMember>();

        audienceLocalBounds = audienceFloor != null ? GetAudienceFloorLocalBounds() : new Bounds(Vector3.zero, Vector3.one);
        CacheSpatialDelays();
        ConfigurePoseFamilies();
        ConfigureInstancedRendering();
        ResetHypeState();
        CacheDrawOrder();
        LogRenderDiagnosticsOnce();
    }

    private void LateUpdate()
    {
        DrawInstancedMembers();
    }

    private void OnEnable()
    {
        SubscribeToBeatEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromBeatEvents();
    }

    private void OnDestroy()
    {
        ClearSpawnedAudienceInstances();

        if (Application.isPlaying)
        {
            if (instancedMaterial != null)
                Destroy(instancedMaterial);
            if (fallbackRuntimeMaterial != null)
                Destroy(fallbackRuntimeMaterial);
        }
    }

    private void CacheDrawOrder()
    {
        if (members == null || members.Length == 0)
        {
            sortedDrawMembers = System.Array.Empty<AudienceMember>();
            return;
        }

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
            renderCamera = Camera.current;

        if (renderCamera == null)
        {
            sortedDrawMembers = (AudienceMember[])members.Clone();
            return;
        }

        Vector3 camPos = renderCamera.transform.position;
        Vector3 camForward = renderCamera.transform.forward;

        drawEntries.Clear();

        for (int i = 0; i < members.Length; i++)
        {
            AudienceMember member = members[i];
            if (member == null)
                continue;

            float depth = Vector3.Dot(camForward, member.transform.position - camPos);
            drawEntries.Add(new AudienceDrawEntry
            {
                Member = member,
                Depth = depth
            });
        }

        drawEntries.Sort((a, b) => b.Depth.CompareTo(a.Depth));

        sortedDrawMembers = new AudienceMember[drawEntries.Count];
        for (int i = 0; i < drawEntries.Count; i++)
            sortedDrawMembers[i] = drawEntries[i].Member;
    }

    private void SpawnAudienceFromPrefabs()
    {
        if (!Application.isPlaying)
            return;

        ClearSpawnedAudienceInstances();

        if (audiencePrefabs == null || audiencePrefabs.Length == 0 || audienceSize <= 0 || audienceFloor == null)
            return;

        Bounds localBounds = GetAudienceFloorLocalBounds();
        float floorY = localBounds.center.y;
        float floorYaw = audienceFloor.rotation.eulerAngles.y;
        if (audienceSize > MaxInstancesPerDraw) audienceSize = MaxInstancesPerDraw;
        for (int i = 0; i < audienceSize; i++)
        {
            GameObject prefab = audiencePrefabs[UnityEngine.Random.Range(0, audiencePrefabs.Length)];
            if (prefab == null)
                continue;

            float localX = UnityEngine.Random.Range(localBounds.min.x, localBounds.max.x);
            float localZ = UnityEngine.Random.Range(localBounds.min.z, localBounds.max.z);
            Vector3 localPosition = new Vector3(localX, floorY, localZ);
            Quaternion spawnRotation = Quaternion.Euler(0f, floorYaw, 0f);

            GameObject instance = Instantiate(prefab, audienceFloor.TransformPoint(localPosition), spawnRotation, audienceFloor);
            if (instance != null)
                spawnedAudienceInstances.Add(instance);
        }
    }

    private void ClearSpawnedAudienceInstances()
    {
        if (spawnedAudienceInstances.Count == 0)
            return;

        for (int i = spawnedAudienceInstances.Count - 1; i >= 0; i--)
        {
            GameObject instance = spawnedAudienceInstances[i];
            if (instance == null)
                continue;

            Destroy(instance);
        }

        spawnedAudienceInstances.Clear();
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

    private void SubscribeToBeatEvents()
    {
        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (beatPlay != null)
        {
            beatPlay.BeatTotalDetected += OnAudienceBeatDetected;
            beatPlay.HypeDetected += OnBeatPlayHypeDetected;
            beatPlay.SongChanged += OnBeatPlaySongChanged;
            beatPlay.SongLooped += OnBeatPlaySongLooped;
            beatPlay.SongSeeked += OnBeatPlaySongSeeked;
            subscribedToBeatPlay = true;
            return;
        }

        if (useGlobalBeatFallback)
        {
            BeatPlay.OnBeatDetected += OnGlobalAudienceBeatDetected;
            subscribedToGlobalBeat = true;
        }
    }

    private void UnsubscribeFromBeatEvents()
    {
        if (subscribedToBeatPlay && beatPlay != null)
        {
            beatPlay.BeatTotalDetected -= OnAudienceBeatDetected;
            beatPlay.HypeDetected -= OnBeatPlayHypeDetected;
            beatPlay.SongChanged -= OnBeatPlaySongChanged;
            beatPlay.SongLooped -= OnBeatPlaySongLooped;
            beatPlay.SongSeeked -= OnBeatPlaySongSeeked;
        }

        if (subscribedToGlobalBeat)
            BeatPlay.OnBeatDetected -= OnGlobalAudienceBeatDetected;

        subscribedToBeatPlay = false;
        subscribedToGlobalBeat = false;
    }

    private void OnGlobalAudienceBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        float songTime = beatPlay != null ? beatPlay.CurrentSongTime : Time.time;
        OnAudienceBeatDetected(beatType, intensity, songTime);
    }

    private void OnAudienceBeatDetected(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (currentMoshZone)
        {
            TriggerAudienceReaction(AudienceMember.MotionStyle.Jump, PoseGroup.Cheer, intensity * 1.4f, 1f, true, timestamp);
            return;
        }

        switch (currentHypeState)
        {
            case HypeState.Low:
                HandleLowHypeBeat(beatType, intensity, timestamp);
                break;
            case HypeState.Medium:
                HandleMediumHypeBeat(beatType, intensity, timestamp);
                break;
            default:
                HandleHighHypeBeat(beatType, intensity, timestamp);
                break;
        }
    }

    private void OnBeatPlayHypeDetected(HypeState hypeState, bool isMoshZone, float timestamp)
    {
        currentHypeState = hypeState;
        currentMoshZone = isMoshZone;
    }

    private void OnBeatPlaySongChanged(BeatData previousData, BeatData newData)
    {
        ResetHypeState();
    }

    private void OnBeatPlaySongLooped(int loopCount, float fromTime, float toTime)
    {
        ResetHypeState();
    }

    private void OnBeatPlaySongSeeked(float fromTime, float toTime)
    {
        ResetHypeState();
    }

    public void OnKickBeat(float strength)
    {
        TriggerAudienceReaction(AudienceMember.MotionStyle.HeadBob, PoseGroup.Idle, strength, 0.75f, false, CurrentAudienceSongTime);
    }

    public void OnSnareBeat(float strength)
    {
        TriggerAudienceReaction(AudienceMember.MotionStyle.Clap, PoseGroup.Clap, strength * 0.8f, 0.45f, false, CurrentAudienceSongTime);
    }

    public void OnHighBeat(float strength)
    {
        TriggerAudienceReaction(AudienceMember.MotionStyle.Wave, PoseGroup.Wave, strength * 0.65f, 0.35f, false, CurrentAudienceSongTime);
    }

    public void OnDropMoment(float strength)
    {
        TriggerAudienceReaction(AudienceMember.MotionStyle.Jump, PoseGroup.Cheer, strength * 1.5f, 1f, true, CurrentAudienceSongTime);
    }

    private void HandleLowHypeBeat(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (beatType == BeatDetection.BeatType.Kick || beatType == BeatDetection.BeatType.BassDrum)
            TriggerAudienceReaction(AudienceMember.MotionStyle.HeadBob, PoseGroup.Idle, intensity * 0.6f, 0.45f, false, timestamp);
    }

    private void HandleMediumHypeBeat(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (beatType == BeatDetection.BeatType.Kick || beatType == BeatDetection.BeatType.BassDrum)
            TriggerAudienceReaction(AudienceMember.MotionStyle.HeadBob, PoseGroup.Idle, intensity, 0.75f, false, timestamp);
        else if (beatType == BeatDetection.BeatType.Snare)
            TriggerAudienceReaction(AudienceMember.MotionStyle.Clap, PoseGroup.Clap, intensity, 0.65f, false, timestamp);
        else if (beatType == BeatDetection.BeatType.HiHat || beatType == BeatDetection.BeatType.Cymbal)
            TriggerAudienceReaction(AudienceMember.MotionStyle.Wave, PoseGroup.Wave, intensity, 0.45f, false, timestamp);
    }

    private void HandleHighHypeBeat(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (beatType == BeatDetection.BeatType.Kick ||
            beatType == BeatDetection.BeatType.BassDrum ||
            beatType == BeatDetection.BeatType.Energy)
        {
            TriggerAudienceReaction(AudienceMember.MotionStyle.Jump, PoseGroup.Cheer, intensity * 1.2f, 0.9f, true, timestamp);
        }
        else if (beatType == BeatDetection.BeatType.Snare)
        {
            TriggerAudienceReaction(AudienceMember.MotionStyle.Clap, PoseGroup.Clap, intensity, 0.75f, false, timestamp);
        }
        else if (beatType == BeatDetection.BeatType.HiHat || beatType == BeatDetection.BeatType.Cymbal)
        {
            TriggerAudienceReaction(AudienceMember.MotionStyle.Wave, PoseGroup.Wave, intensity, 0.65f, false, timestamp);
        }
    }

    private void TriggerAudienceReaction(AudienceMember.MotionStyle motionStyle, PoseGroup poseGroup, float strength, float chance, bool ignorePoseCooldown, float beatTimestamp)
    {
        if (members == null)
            return;

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null || UnityEngine.Random.value > chance)
                continue;

            float delay = UnityEngine.Random.Range(0f, Mathf.Max(0f, randomLatencyMax)) + GetSpatialDelay(i, members[i]);
            float triggerSongTime = beatTimestamp + audienceTimingOffset + delay;
            members[i].SetPose(i, motionStyle, poseGroup, strength, ignorePoseCooldown, triggerSongTime);
        }
    }

    private void CacheSpatialDelays()
    {
        if (members == null || members.Length == 0)
        {
            cachedSpatialDelays = System.Array.Empty<float>();
            return;
        }

        if (cachedSpatialDelays == null || cachedSpatialDelays.Length != members.Length)
            cachedSpatialDelays = new float[members.Length];

        for (int i = 0; i < members.Length; i++)
            cachedSpatialDelays[i] = CalculateSpatialDelay(members[i]);
    }

    private float GetSpatialDelay(int memberIndex, AudienceMember member)
    {
        if (cachedSpatialDelays != null && memberIndex >= 0 && memberIndex < cachedSpatialDelays.Length)
            return cachedSpatialDelays[memberIndex];

        return CalculateSpatialDelay(member);
    }

    private float CalculateSpatialDelay(AudienceMember member)
    {
        if (audienceFloor == null || member == null)
            return 0f;

        Vector3 localPosition = audienceFloor.InverseTransformPoint(member.transform.position);
        float row01 = Mathf.InverseLerp(audienceLocalBounds.min.z, audienceLocalBounds.max.z, localPosition.z);

        if (!frontIsLowerLocalZ)
            row01 = 1f - row01;

        return Mathf.Clamp01(row01) * Mathf.Max(0f, backRowDelay);
    }

    private HypeAnalysis AnalyzeHype(float timestamp)
    {
        IReadOnlyList<BeatEvent> beatEvents = beatPlay != null ? beatPlay.BeatEvents : null;
        if (beatEvents == null || beatEvents.Count == 0)
            return new HypeAnalysis(HypeState.Medium, false);

        float windowEnd = timestamp + Mathf.Max(0.1f, lookAheadSeconds);
        int beatCount = 0;
        float intensitySum = 0f;
        float firstHalfIntensity = 0f;
        float secondHalfIntensity = 0f;
        HashSet<BeatDetection.BeatType> highTypes = new HashSet<BeatDetection.BeatType>();
        HashSet<BeatDetection.BeatType> simultaneousTypes = new HashSet<BeatDetection.BeatType>();

        for (int i = 0; i < beatEvents.Count; i++)
        {
            BeatEvent beatEvent = beatEvents[i];

            if (beatEvent.timestamp < timestamp)
                continue;

            if (beatEvent.timestamp > windowEnd)
                break;

            beatCount++;
            intensitySum += beatEvent.intensity;

            if (beatEvent.timestamp < timestamp + lookAheadSeconds * 0.5f)
                firstHalfIntensity += beatEvent.intensity;
            else
                secondHalfIntensity += beatEvent.intensity;

            if (beatEvent.intensity >= highAverageIntensity)
                highTypes.Add(beatEvent.beatType);

            if (Mathf.Abs(beatEvent.timestamp - timestamp) <= simultaneousBeatWindow && beatEvent.intensity >= highAverageIntensity)
                simultaneousTypes.Add(beatEvent.beatType);
        }

        float beatsPerSecond = beatCount / Mathf.Max(0.1f, lookAheadSeconds);
        float averageIntensity = beatCount > 0 ? intensitySum / beatCount : 0f;
        bool rising = secondHalfIntensity > firstHalfIntensity * 1.15f;
        bool quiet = beatCount <= quietWindowBeatThreshold;
        bool high = averageIntensity >= highAverageIntensity &&
                    highTypes.Count >= highUniqueBeatTypes &&
                    (rising || beatsPerSecond >= mediumBeatsPerSecond);

        HypeState state = HypeState.Medium;
        if (quiet)
            state = HypeState.Low;
        else if (high)
            state = HypeState.High;

        return new HypeAnalysis(state, simultaneousTypes.Count >= moshUniqueBeatTypes);
    }

    private HypeAnalysis ResolveHype(float timestamp)
    {
        if (TryGetPrecomputedHype(timestamp, out HypeAnalysis precomputed))
            return precomputed;

        return AnalyzeHype(timestamp);
    }

    private bool TryGetPrecomputedHype(float timestamp, out HypeAnalysis hype)
    {
        List<HypeChange> hypeEvents = beatPlay != null && beatPlay.LoadedBeatData != null
            ? beatPlay.LoadedBeatData.hypeEvents
            : null;

        if (hypeEvents == null || hypeEvents.Count == 0)
        {
            hype = default;
            return false;
        }

        if (!hypeEventsSorted)
        {
            hypeEvents.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            hypeEventsSorted = true;
        }

        int eventIndex = FindLastHypeIndexAtOrBefore(hypeEvents, timestamp);
        if (eventIndex >= 0)
        {
            HypeChange change = hypeEvents[eventIndex];
            currentHypeState = change.newState;
            currentMoshZone = change.isMoshZone;
            hypeEventIndex = eventIndex + 1;
        }
        else
        {
            currentHypeState = HypeState.Medium;
            currentMoshZone = false;
            hypeEventIndex = 0;
        }

        previousBeatTimestamp = timestamp;
        hype = new HypeAnalysis(currentHypeState, currentMoshZone);
        return true;
    }

    private static int FindLastHypeIndexAtOrBefore(IReadOnlyList<HypeChange> hypeEvents, float songTime)
    {
        if (hypeEvents == null || hypeEvents.Count == 0)
            return -1;

        int low = 0;
        int high = hypeEvents.Count - 1;
        int result = -1;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            if (hypeEvents[mid].timestamp <= songTime)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    private void ResetHypeState()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("AudienceManager ResetHypeState");
        #endif
        currentHypeState = HypeState.Low;
        currentMoshZone = false;
        hypeEventIndex = 0;
        previousBeatTimestamp = 0f;
        hypeEventsSorted = false;
    }

    private void ConfigurePoseFamilies()
    {
        if (members == null)
            return;

        nextPoseChangeTimes = new float[members.Length];

        for (int i = 0; i < members.Length; i++)
        {
            nextPoseChangeTimes[i] = 0f;

            if (members[i] != null)
                members[i].SetFrame(GetFrameForMember(members[i].AtlasColumn, PoseGroup.Idle));
        }
    }

    public void TrySetPose(int memberIndex, PoseGroup poseGroup, bool ignoreCooldown)
    {
        if (members == null || memberIndex < 0 || memberIndex >= members.Length || members[memberIndex] == null)
            return;

        if (!ignoreCooldown && Time.time < nextPoseChangeTimes[memberIndex])
            return;

        int atlasColumn = members[memberIndex].AtlasColumn;
        members[memberIndex].SetFrame(GetFrameForMember(atlasColumn, poseGroup));
        float minHold = Mathf.Min(minPoseHoldTime, maxPoseHoldTime);
        float maxHold = Mathf.Max(minPoseHoldTime, maxPoseHoldTime);
        nextPoseChangeTimes[memberIndex] = Time.time + UnityEngine.Random.Range(minHold, maxHold);
    }

    private int GetFrameForMember(int memberIndex, PoseGroup poseGroup)
    {
        if (memberIndex < 0)
            return 0;

        switch (poseGroup)
        {
            case PoseGroup.Clap:
                return GetFrame(clapFrames, memberIndex);
            case PoseGroup.Cheer:
                return GetFrame(cheerFrames, memberIndex);
            case PoseGroup.Wave:
                return GetFrame(waveFrames, memberIndex);
            default:
                return GetFrame(idleFrames, memberIndex);
        }
    }

    private static int GetFrame(int[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
            return 0;

        return frames[Mathf.Abs(index) % frames.Length];
    }

    private readonly struct HypeAnalysis
    {
        public readonly HypeState State;
        public readonly bool IsMoshBeat;

        public HypeAnalysis(HypeState state, bool isMoshBeat)
        {
            State = state;
            IsMoshBeat = isMoshBeat;
        }
    }

    private void ConfigureInstancedRendering()
    {
        if (!Application.isPlaying || !useInstancedRendering || members == null || members.Length == 0)
        {
            ApplyRenderQueueToNonInstancedMembers();
            return;
        }

        AudienceMember firstValidMember = null;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null)
                continue;

            if (members[i].Mesh != null && members[i].SharedMaterial != null)
            {
                firstValidMember = members[i];
                break;
            }
        }

        instancedMesh = firstValidMember != null ? firstValidMember.Mesh : null;
        Material sourceMaterial = firstValidMember != null ? firstValidMember.SharedMaterial : null;

        Shader instancedShader = Shader.Find(useLit ? InstancedLitShaderName : InstancedUnlitShaderName);

        if (instancedMesh == null || sourceMaterial == null || instancedShader == null)
        {
            useInstancedRendering = false;
            ApplyRenderQueueToNonInstancedMembers();
            return;
        }

        instancedMaterial = new Material(instancedShader)
        {
            name = $"{sourceMaterial.name} (Instanced Runtime)",
            enableInstancing = true
        };

        int baseQueue = sourceMaterial.renderQueue > 0 ? sourceMaterial.renderQueue : 3000;
        instancedMaterial.renderQueue = Mathf.Clamp(baseQueue + audienceRenderQueueOffset, 1000, 5000);

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

    private void ApplyRenderQueueToNonInstancedMembers()
    {
        if (!Application.isPlaying || members == null || members.Length == 0)
            return;

        Material sourceMaterial = null;
        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null)
                continue;

            sourceMaterial = members[i].SharedMaterial;
            if (sourceMaterial != null)
                break;
        }

        int baseQueue = sourceMaterial != null && sourceMaterial.renderQueue > 0 ? sourceMaterial.renderQueue : 3000;
        int targetQueue = Mathf.Clamp(baseQueue + audienceRenderQueueOffset, 1000, 5000);
        Material fallbackMaterial = BuildFallbackRuntimeMaterial(sourceMaterial, targetQueue);

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] == null)
                continue;

            members[i].SetRuntimeRendererEnabled(true);
            if (fallbackMaterial != null)
                members[i].SetRuntimeSharedMaterial(fallbackMaterial);
            else
                members[i].SetRuntimeRenderQueue(targetQueue);
        }
    }

    private Material BuildFallbackRuntimeMaterial(Material sourceMaterial, int targetQueue)
    {
        Shader fallbackShader = Shader.Find(useLit ? InstancedLitShaderName : InstancedUnlitShaderName);
        if (fallbackShader == null)
            fallbackShader = sourceMaterial != null ? sourceMaterial.shader : null;

        if (fallbackShader == null)
            return sourceMaterial;

        if (fallbackRuntimeMaterial == null || fallbackRuntimeMaterial.shader != fallbackShader)
        {
            fallbackRuntimeMaterial = new Material(fallbackShader)
            {
                name = $"Audience Fallback Runtime ({fallbackShader.name})"
            };
        }

        if (sourceMaterial != null)
        {
            if (sourceMaterial.HasProperty(BaseMap))
                fallbackRuntimeMaterial.SetTexture(BaseMap, sourceMaterial.GetTexture(BaseMap));
            else if (sourceMaterial.HasProperty(MainTex))
                fallbackRuntimeMaterial.SetTexture(BaseMap, sourceMaterial.GetTexture(MainTex));

            if (sourceMaterial.HasProperty(BaseColor))
                fallbackRuntimeMaterial.SetColor(BaseColor, sourceMaterial.GetColor(BaseColor));
            else if (sourceMaterial.HasProperty(ColorId))
                fallbackRuntimeMaterial.SetColor(BaseColor, sourceMaterial.GetColor(ColorId));
        }

        fallbackRuntimeMaterial.renderQueue = targetQueue;
        return fallbackRuntimeMaterial;
    }

    private void LogRenderDiagnosticsOnce()
    {
        if (!logRenderPathOnDevice)
            return;

#if UNITY_EDITOR
        return;
#else
        int instancedQueue = instancedMaterial != null ? instancedMaterial.renderQueue : -1;
        int memberQueue = -1;
        if (members != null)
        {
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == null)
                    continue;

                memberQueue = members[i].GetCurrentRenderQueue();
                break;
            }
        }

        Debug.Log($"[AudienceManager] RenderPath={ (useInstancedRendering && instancedMaterial != null ? "Instanced" : "NonInstanced") }, InstancedQueue={instancedQueue}, MemberQueue={memberQueue}, QueueOffset={audienceRenderQueueOffset}, UseLit={useLit}");
#endif
    }

    private void DrawInstancedMembers()
    {
        if (!Application.isPlaying || !useInstancedRendering || instancedMesh == null || instancedMaterial == null)
            return;

        AudienceMember[] drawMembers = sortedDrawMembers != null && sortedDrawMembers.Length > 0 ? sortedDrawMembers : members;
        if (drawMembers == null || drawMembers.Length == 0)
            return;

        int count = 0;
        for (int i = 0; i < drawMembers.Length && count < MaxInstancesPerDraw; i++)
        {
            AudienceMember member = drawMembers[i];

            if (member == null || !member.gameObject.activeInHierarchy)
                continue;

            matrices[count] = member.RenderMatrix;
            atlasSTs[count] = member.AtlasST;
            count++;
        }

        if (count == 0)
            return;

        instancedBlock.SetVectorArray(AudienceAtlasST, atlasSTs);
        Graphics.DrawMeshInstanced(instancedMesh, 0, instancedMaterial, matrices, count, instancedBlock);
    }
}
