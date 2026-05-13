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
    [SerializeField] private float lastBeatTimestamp = -999f;
    private bool hypeEventsSorted;
    private float previousSongTime = -1f;
    private BeatData cachedBeatData;

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

        ResetHypeState();

        if (beatPlay != null)
        {
            beatPlay.BeatTotalDetected += OnBeatDetected;
            beatPlay.SongChanged += OnSongChanged;
            beatPlay.SongLooped += OnSongLooped;
            beatPlay.SongSeeked += OnSongSeeked;
        }
    }

    private void OnDisable()
    {
        if (beatPlay != null)
        {
            beatPlay.BeatTotalDetected -= OnBeatDetected;
            beatPlay.SongChanged -= OnSongChanged;
            beatPlay.SongLooped -= OnSongLooped;
            beatPlay.SongSeeked -= OnSongSeeked;
        }
    }

    private void Update()
    {
        if (beatPlay == null)
            return;

        if (beatPlay.LoadedBeatData != cachedBeatData)
        {
            cachedBeatData = beatPlay.LoadedBeatData;
            ResetHypeState();
        }

        float songTime = beatPlay.CurrentSongTime;

        // Handle looping/restart: if timeline wraps backwards, clear carried hype state.
        if (previousSongTime >= 0f && songTime + 0.05f < previousSongTime)
            ResetHypeState();

        previousSongTime = songTime;
        UpdateHypeState(songTime + performerLeadTime);
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

    private void OnSongChanged(BeatData previousData, BeatData newData)
    {
        cachedBeatData = newData;
        ResetHypeState();
    }

    private void OnSongLooped(int loopCount, float fromTime, float toTime)
    {
        ResetHypeState();
    }

    private void OnSongSeeked(float fromTime, float toTime)
    {
        ResetHypeState();
    }

    private void UpdateHypeState(float timestamp)
    {
        BeatData data = beatPlay != null ? beatPlay.LoadedBeatData : null;
        List<HypeChange> hypeEvents = data != null ? data.hypeEvents : null;

        if (hypeEvents == null || hypeEvents.Count == 0)
            return;

        if (!hypeEventsSorted)
        {
            hypeEvents.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
            hypeEventsSorted = true;
        }

        int eventIndex = FindLastHypeIndexAtOrBefore(hypeEvents, timestamp);
        if (eventIndex >= 0)
            currentHypeState = hypeEvents[eventIndex].newState;
    }

    private void ResetHypeState()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("BandPerformanceDirector ResetHypeState");
        #endif

        currentHypeState = HypeState.Medium;
        hypeEventsSorted = false;
        previousSongTime = -1f;
        lastBeatTimestamp = -999f;
    }

    private static int FindLastHypeIndexAtOrBefore(IReadOnlyList<HypeChange> hypeEvents, float songTime)
    {
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
}
