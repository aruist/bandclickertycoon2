using System.Collections.Generic;
using UnityEngine;

public class BandPerformanceDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatPlay beatPlay;
    [SerializeField] private BandPerformerMember[] performers;

    [Header("Timing")]
    [SerializeField] private float performerLeadTime = 0.02f;
    [SerializeField] private float accentThreshold = 0.75f;
    [SerializeField] private float minBeatSeparation = 0.02f;

    [Header("Debug")]
    [SerializeField] private bool logTriggers = false;

    private HypeState currentHypeState = HypeState.Medium;
    private int hypeEventIndex;
    private float lastBeatTimestamp = -999f;

    private void Awake()
    {
        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (performers == null || performers.Length == 0)
            performers = GetComponentsInChildren<BandPerformerMember>(true);

        ResetHypeState();
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

    private void Update()
    {
        if (beatPlay == null)
            return;

        float songTime = beatPlay.CurrentSongTime + performerLeadTime;
        UpdateHypeState(songTime);
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (performers == null || performers.Length == 0)
            return;

        if (timestamp - lastBeatTimestamp < minBeatSeparation)
            return;

        lastBeatTimestamp = timestamp;
        float triggerTime = timestamp + performerLeadTime;
        UpdateHypeState(triggerTime);
        bool isAccent = intensity >= accentThreshold || beatType == BeatDetection.BeatType.Energy;

        for (int i = 0; i < performers.Length; i++)
        {
            BandPerformerMember performer = performers[i];
            if (performer == null || !performer.gameObject.activeInHierarchy)
                continue;

            performer.ApplyBeat(beatType, intensity, isAccent, currentHypeState);
        }

        if (logTriggers)
            Debug.Log($"[BandPerformanceDirector] Beat={beatType}, intensity={intensity:0.00}, hype={currentHypeState}, accent={isAccent}");
    }

    private void UpdateHypeState(float timestamp)
    {
        BeatData data = beatPlay != null ? beatPlay.LoadedBeatData : null;
        IReadOnlyList<HypeChange> hypeEvents = data != null ? data.hypeEvents : null;

        if (hypeEvents == null || hypeEvents.Count == 0)
            return;

        if (hypeEventIndex >= hypeEvents.Count || (hypeEventIndex > 0 && timestamp < hypeEvents[hypeEventIndex - 1].timestamp))
            hypeEventIndex = FindFirstHypeIndexAtOrAfter(hypeEvents, timestamp);

        while (hypeEventIndex < hypeEvents.Count && hypeEvents[hypeEventIndex].timestamp <= timestamp)
        {
            currentHypeState = hypeEvents[hypeEventIndex].newState;
            hypeEventIndex++;
        }
    }

    private void ResetHypeState()
    {
        currentHypeState = HypeState.Medium;
        hypeEventIndex = 0;
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
