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
    [SerializeField] private float minBeatSeparation = 0.06f;

    [Header("Debug")]
    [SerializeField] private bool logTriggers = false;

    private HypeState currentHypeState = HypeState.Medium;
    [SerializeField] private float lastBeatTimestamp = -999f;
    private float previousSongTime = -1f;

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
            beatPlay.HypeDetected += OnHypeDetected;
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
            beatPlay.HypeDetected -= OnHypeDetected;
            beatPlay.SongChanged -= OnSongChanged;
            beatPlay.SongLooped -= OnSongLooped;
            beatPlay.SongSeeked -= OnSongSeeked;
        }
    }

    private void Update()
    {
        if (beatPlay == null)
            return;

        float songTime = beatPlay.CurrentSongTime;

        // Handle looping/restart: if timeline wraps backwards, clear carried hype state.
        if (previousSongTime >= 0f && songTime + 0.05f < previousSongTime)
            ResetHypeState();

        previousSongTime = songTime;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity, float timestamp)
    {
        if (performers == null || performers.Length == 0)
            return;

        if (timestamp - lastBeatTimestamp < minBeatSeparation)
            return;

        lastBeatTimestamp = timestamp;
        float triggerTime = timestamp + performerLeadTime;
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

    private void OnHypeDetected(HypeState hypeState, bool isMoshZone, float timestamp)
    {
        currentHypeState = hypeState;
    }

    private void OnSongChanged(BeatData previousData, BeatData newData)
    {
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

    private void ResetHypeState()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("BandPerformanceDirector ResetHypeState");
        #endif

        currentHypeState = HypeState.Low;
        previousSongTime = -1f;
        lastBeatTimestamp = -999f;
    }
}
