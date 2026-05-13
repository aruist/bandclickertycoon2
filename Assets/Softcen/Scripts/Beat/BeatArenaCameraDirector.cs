using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class BeatArenaCameraDirector : MonoBehaviour
{
    public enum ShotType
    {
        Wide,
        SideSweep,
        PushIn
    }

    [Serializable]
    public class Shot
    {
        public string name = "Shot";
        public ShotType type = ShotType.Wide;
        public CinemachineCamera camera;
        public Transform anchor;
        public bool allowLow = true;
        public bool allowMedium = true;
        public bool allowHigh = true;
        public float minHoldSeconds = 2.5f;
        public float maxHoldSeconds = 5f;
        public float horizontalAmplitude = 0.8f;
        public float horizontalFrequency = 0.3f;
        public float pushAmplitude = 1.5f;
        [NonSerialized] public Vector3 baseLocalPosition;
        [NonSerialized] public Quaternion baseLocalRotation;
        [NonSerialized] public float phase;
    }

    [Header("References")]
    [SerializeField] private BeatPlay beatPlay;
    [SerializeField] private Transform stageTarget;
    [SerializeField] private Transform rigRoot;

    [Header("Blend Behavior")]
    [SerializeField] private float beatAccentThreshold = 0.75f;
    [SerializeField] private float earlySwitchChanceOnAccent = 0.4f;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    [Header("Framing Rules")]
    [SerializeField] private float lookAtHeightOffset = 1.8f;
    [SerializeField] private bool keepCamerasBehindStage = true;

    [Header("Shots")]
    [SerializeField] private List<Shot> shots = new List<Shot>();

    private int activeShotIndex = -1;
    private float nextSwitchSongTime;
    private HypeState currentHypeState = HypeState.Medium;
    private int hypeEventIndex;

    private void Awake()
    {
        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (rigRoot == null)
            rigRoot = transform;

        if (stageTarget == null)
        {
            GameObject stage = GameObject.Find("Stage");
            if (stage != null)
                stageTarget = stage.transform;
        }

        EnsureDefaultShots();
        CacheShotBases();
        InitializePriorities();
    }

    private void OnEnable()
    {
        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (beatPlay != null)
            beatPlay.BeatTotalDetected += OnBeatDetected;
    }

    private void OnDisable()
    {
        if (beatPlay != null)
            beatPlay.BeatTotalDetected -= OnBeatDetected;
    }

    private void Start()
    {
        EnsureDefaultShots();
        if (shots.Count > 0)
            ActivateShot(0, beatPlay != null ? beatPlay.CurrentSongTime : 0f);
    }

    private void Update()
    {
        float songTime = beatPlay != null ? beatPlay.CurrentSongTime : Time.time;
        UpdateHype(songTime);
        UpdateActiveShotMotion(songTime);

        if (activeShotIndex < 0 || activeShotIndex >= shots.Count)
            return;

        if (songTime >= nextSwitchSongTime)
            TrySwitchShot(songTime, false);
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        UpdateHype(timestamp);

        if (intensity < beatAccentThreshold)
            return;

        if (UnityEngine.Random.value <= earlySwitchChanceOnAccent)
            TrySwitchShot(timestamp, true);
    }

    private void TrySwitchShot(float songTime, bool forceEarly)
    {
        if (shots.Count <= 1)
            return;

        int currentIndex = Mathf.Clamp(activeShotIndex, 0, shots.Count - 1);
        Shot current = shots[currentIndex];

        if (!forceEarly && songTime < nextSwitchSongTime)
            return;

        List<int> candidates = new List<int>();
        for (int i = 0; i < shots.Count; i++)
        {
            if (i == currentIndex)
                continue;

            Shot candidate = shots[i];
            if (candidate == null || candidate.camera == null || candidate.anchor == null)
                continue;

            if (IsAllowedByHype(candidate, currentHypeState))
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return;

        int selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        ActivateShot(selected, songTime);
    }

    private void ActivateShot(int shotIndex, float songTime)
    {
        if (shotIndex < 0 || shotIndex >= shots.Count)
            return;

        activeShotIndex = shotIndex;

        for (int i = 0; i < shots.Count; i++)
        {
            Shot shot = shots[i];
            if (shot == null || shot.camera == null)
                continue;

            shot.camera.Priority.Value = (i == shotIndex) ? activePriority : inactivePriority;
            if (i == shotIndex)
                shot.camera.Prioritize();
        }

        Shot active = shots[shotIndex];
        float minHold = Mathf.Max(0.5f, active.minHoldSeconds);
        float maxHold = Mathf.Max(minHold, active.maxHoldSeconds);
        nextSwitchSongTime = songTime + UnityEngine.Random.Range(minHold, maxHold);
    }

    private void UpdateActiveShotMotion(float songTime)
    {
        if (activeShotIndex < 0 || activeShotIndex >= shots.Count)
            return;

        Shot shot = shots[activeShotIndex];
        if (shot == null || shot.anchor == null || stageTarget == null)
            return;

        Vector3 localPos = shot.baseLocalPosition;

        if (shot.type == ShotType.SideSweep)
        {
            float t = songTime * shot.horizontalFrequency + shot.phase;
            localPos.x += Mathf.Sin(t) * shot.horizontalAmplitude;
        }
        else if (shot.type == ShotType.PushIn)
        {
            float t = songTime * shot.horizontalFrequency + shot.phase;
            localPos.z += Mathf.Sin(t) * shot.pushAmplitude;
        }
        else
        {
            float t = songTime * shot.horizontalFrequency * 0.6f + shot.phase;
            localPos.x += Mathf.Sin(t) * (shot.horizontalAmplitude * 0.25f);
        }

        shot.anchor.localPosition = localPos;
        LookAtStage(shot.anchor);
    }

    private void LookAtStage(Transform anchor)
    {
        Vector3 target = stageTarget.position + Vector3.up * lookAtHeightOffset;
        Vector3 direction = target - anchor.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        if (keepCamerasBehindStage && rigRoot != null)
        {
            Vector3 rootToAnchor = anchor.position - rigRoot.position;
            float frontDot = Vector3.Dot(rootToAnchor.normalized, stageTarget.forward);
            if (frontDot > 0.95f)
                anchor.position = rigRoot.position - stageTarget.forward * 1.5f + Vector3.up * rootToAnchor.y;
        }

        anchor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void UpdateHype(float timestamp)
    {
        if (beatPlay == null || beatPlay.LoadedBeatData == null || beatPlay.LoadedBeatData.hypeEvents == null)
            return;

        IReadOnlyList<HypeChange> hypeEvents = beatPlay.LoadedBeatData.hypeEvents;
        if (hypeEvents.Count == 0)
            return;

        if (hypeEventIndex >= hypeEvents.Count || (hypeEventIndex > 0 && timestamp < hypeEvents[hypeEventIndex - 1].timestamp))
            hypeEventIndex = FindFirstHypeIndexAtOrAfter(hypeEvents, timestamp);

        while (hypeEventIndex < hypeEvents.Count && hypeEvents[hypeEventIndex].timestamp <= timestamp)
        {
            currentHypeState = hypeEvents[hypeEventIndex].newState;
            hypeEventIndex++;
        }
    }

    private void CacheShotBases()
    {
        for (int i = 0; i < shots.Count; i++)
        {
            Shot shot = shots[i];
            if (shot == null || shot.anchor == null)
                continue;

            shot.baseLocalPosition = shot.anchor.localPosition;
            shot.baseLocalRotation = shot.anchor.localRotation;
            shot.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void EnsureDefaultShots()
    {
        if (shots.Count > 0)
            return;

        if (rigRoot == null)
            return;

        TryAddDefaultShot("CM_Wide", ShotType.Wide, true, true, false, 3.0f, 5.5f, 0.5f, 0.22f, 0.0f);
        TryAddDefaultShot("CM_SideSweep", ShotType.SideSweep, false, true, true, 2.5f, 4.0f, 1.25f, 0.42f, 0.0f);
        TryAddDefaultShot("CM_PushIn", ShotType.PushIn, false, true, true, 2.0f, 3.2f, 0.35f, 0.55f, 1.35f);
    }

    private void TryAddDefaultShot(
        string cameraName,
        ShotType shotType,
        bool allowLow,
        bool allowMedium,
        bool allowHigh,
        float minHold,
        float maxHold,
        float horizontalAmplitude,
        float horizontalFrequency,
        float pushAmplitude)
    {
        Transform cameraTransform = rigRoot.Find(cameraName);
        if (cameraTransform == null)
            return;

        CinemachineCamera camera = cameraTransform.GetComponent<CinemachineCamera>();
        if (camera == null)
            return;

        shots.Add(new Shot
        {
            name = cameraName,
            type = shotType,
            camera = camera,
            anchor = cameraTransform,
            allowLow = allowLow,
            allowMedium = allowMedium,
            allowHigh = allowHigh,
            minHoldSeconds = minHold,
            maxHoldSeconds = maxHold,
            horizontalAmplitude = horizontalAmplitude,
            horizontalFrequency = horizontalFrequency,
            pushAmplitude = pushAmplitude
        });
    }

    private void InitializePriorities()
    {
        for (int i = 0; i < shots.Count; i++)
        {
            Shot shot = shots[i];
            if (shot == null || shot.camera == null)
                continue;

            shot.camera.Priority.Value = inactivePriority;
        }
    }

    private static bool IsAllowedByHype(Shot shot, HypeState hypeState)
    {
        switch (hypeState)
        {
            case HypeState.Low:
                return shot.allowLow;
            case HypeState.High:
                return shot.allowHigh;
            default:
                return shot.allowMedium;
        }
    }

    private static int FindFirstHypeIndexAtOrAfter(IReadOnlyList<HypeChange> hypeEvents, float songTime)
    {
        int low = 0;
        int high = hypeEvents.Count - 1;
        int result = hypeEvents.Count;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            if (hypeEvents[mid].timestamp >= songTime)
            {
                result = mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return result;
    }
}
