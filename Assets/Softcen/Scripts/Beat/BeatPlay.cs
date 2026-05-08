using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime player for pre-recorded beat JSON created by BeatDetection.
/// It does not analyze audio. It only reads beat timestamps and emits events while the AudioSource plays.
/// </summary>
public class BeatPlay : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private TextAsset preRecordedBeatData;
    [SerializeField] private AudioSource audioSource;

    [Header("Playback")]
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool useTimeSamples = true;

    [Tooltip("Positive value fires beat events slightly before the audio timestamp. Useful if visuals feel late.")]
    [SerializeField, Min(0f)] private float visualLeadTime = 0f;

    [Tooltip("Prevents very large bursts if the app resumes after a stall. 0 = unlimited.")]
    [SerializeField, Min(0)] private int maxEventsPerFrame = 0;

    [Header("Debug")]
    [SerializeField] private bool logLoadedData = true;
    [SerializeField] private bool logBeatEvents = false;

    public delegate void BeatEventHandler(BeatDetection.BeatType beatType, float intensity);
    public delegate void BeatEventTotalHandler(BeatDetection.BeatType beatType, float intensity, float timestamp);

    /// <summary>
    /// Prefer subscribing to the instance event when possible.
    /// </summary>
    public event BeatEventHandler BeatDetected;
    public event BeatEventTotalHandler BeatTotalDetected;

    /// <summary>
    /// Optional global event for simple projects. Be careful with duplicate BeatPlay instances.
    /// </summary>
    public static event BeatEventHandler OnBeatDetected;

    public bool HasData => recordedBeatData != null && recordedBeatData.beatEvents != null;
    public int BeatCount => HasData ? recordedBeatData.beatEvents.Count : 0;
    public float AudioLength => recordedBeatData != null ? recordedBeatData.audioLength : 0f;
    public int CurrentBeatIndex => currentBeatIndex;

    private BeatData recordedBeatData;
    private List<BeatEvent> beatEvents;
    private int currentBeatIndex;
    private float previousSongTime;
    private bool wasPlaying;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (loadOnAwake)
            LoadPreRecordedData();
    }

    private void Start()
    {
        if (playOnStart && audioSource != null)
            PlayFromStart();
    }

    private void Update()
    {
        Tick();
    }

    public void LoadPreRecordedData()
    {
        currentBeatIndex = 0;
        previousSongTime = 0f;
        recordedBeatData = null;
        beatEvents = null;

        if (preRecordedBeatData == null)
        {
            Debug.LogError("BeatPlay has no pre-recorded beat data assigned.", this);
            enabled = false;
            return;
        }

        recordedBeatData = JsonUtility.FromJson<BeatData>(preRecordedBeatData.text);

        if (recordedBeatData == null || recordedBeatData.beatEvents == null)
        {
            Debug.LogError($"BeatPlay could not parse beat data from {preRecordedBeatData.name}.", this);
            enabled = false;
            return;
        }

        beatEvents = recordedBeatData.beatEvents;

        // Make playback robust even if the JSON order is not perfect.
        beatEvents.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

        if (logLoadedData)
        {
            Debug.Log($"Loaded {beatEvents.Count} beat events from {preRecordedBeatData.name}. " +
                      $"Clip: {recordedBeatData.clipName}, length: {recordedBeatData.audioLength:0.00}s", this);
        }
    }

    public void PlayFromStart()
    {
        if (audioSource == null)
        {
            Debug.LogError("BeatPlay needs an AudioSource.", this);
            return;
        }

        ResetPlaybackState(0f);
        audioSource.Stop();
        audioSource.time = 0f;
        audioSource.Play();
    }

    public void StopPlayback()
    {
        if (audioSource != null)
            audioSource.Stop();

        ResetPlaybackState(0f);
    }

    public void ResetPlaybackState(float songTime)
    {
        currentBeatIndex = FindFirstBeatIndexAtOrAfter(songTime);
        previousSongTime = songTime;
        wasPlaying = false;
    }

    private void Tick()
    {
        if (audioSource == null || !HasData || beatEvents.Count == 0)
            return;

        if (!audioSource.isPlaying)
        {
            wasPlaying = false;
            return;
        }

        float songTime = GetSongTime();
        float triggerTime = songTime + visualLeadTime;

        // Detect loop, rewind, or manual seek backwards.
        if (wasPlaying && songTime + 0.05f < previousSongTime)
            ResetPlaybackState(songTime);

        // Detect large forward seeks so old missed events are not fired in one burst.
        // Small frame-to-frame movement is handled normally.
        if (wasPlaying && songTime - previousSongTime > 1.0f)
            currentBeatIndex = FindFirstBeatIndexAtOrAfter(previousSongTime);

        int eventsThisFrame = 0;

        while (currentBeatIndex < beatEvents.Count)
        {
            BeatEvent nextBeat = beatEvents[currentBeatIndex];

            if (nextBeat.timestamp > triggerTime)
                break;

            EmitBeat(nextBeat);
            currentBeatIndex++;
            eventsThisFrame++;

            if (maxEventsPerFrame > 0 && eventsThisFrame >= maxEventsPerFrame)
                break;
        }

        if (currentBeatIndex >= beatEvents.Count && audioSource.loop)
        {
            // Wait for AudioSource time to actually wrap before restarting the beat index.
            // This avoids repeatedly restarting at the end of the clip.
            if (songTime < previousSongTime || songTime < 0.1f)
                currentBeatIndex = 0;
        }

        previousSongTime = songTime;
        wasPlaying = true;
    }

    private float GetSongTime()
    {
        if (useTimeSamples && audioSource.clip != null && audioSource.clip.frequency > 0)
            return audioSource.timeSamples / (float)audioSource.clip.frequency;

        return audioSource.time;
    }

    private void EmitBeat(BeatEvent beatEvent)
    {
        float intensity = Mathf.Clamp01(beatEvent.intensity);

        if (logBeatEvents)
        {
            Debug.Log($"Beat {beatEvent.beatType}, time {beatEvent.timestamp:0.000}, intensity {intensity:0.000}", this);
        }

        BeatDetected?.Invoke(beatEvent.beatType, intensity);
        BeatTotalDetected?.Invoke(beatEvent.beatType, intensity, beatEvent.timestamp);
        OnBeatDetected?.Invoke(beatEvent.beatType, intensity);
    }

    private int FindFirstBeatIndexAtOrAfter(float songTime)
    {
        if (beatEvents == null || beatEvents.Count == 0)
            return 0;

        int low = 0;
        int high = beatEvents.Count - 1;
        int result = beatEvents.Count;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);

            if (beatEvents[mid].timestamp >= songTime)
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
