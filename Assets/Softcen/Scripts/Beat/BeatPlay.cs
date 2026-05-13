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
    [SerializeField] private bool useDspClock = true;
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
    public delegate void SongChangedHandler(BeatData previousData, BeatData newData);
    public delegate void SongTimeEventHandler(float fromTime, float toTime);
    public delegate void SongLoopedHandler(int loopCount, float fromTime, float toTime);

    /// <summary>
    /// Prefer subscribing to the instance event when possible.
    /// </summary>
    public event BeatEventHandler BeatDetected;
    public event BeatEventTotalHandler BeatTotalDetected;
    public event SongChangedHandler SongChanged;
    public event SongTimeEventHandler SongSeeked;
    public event SongLoopedHandler SongLooped;

    /// <summary>
    /// Optional global event for simple projects. Be careful with duplicate BeatPlay instances.
    /// </summary>
    public static event BeatEventHandler OnBeatDetected;

    public bool HasData => recordedBeatData != null && recordedBeatData.beatEvents != null;
    public int BeatCount => HasData ? recordedBeatData.beatEvents.Count : 0;
    public float AudioLength => recordedBeatData != null ? recordedBeatData.audioLength : 0f;
    public int CurrentBeatIndex => currentBeatIndex;
    public float CurrentSongTime => audioSource != null && audioSource.isPlaying ? GetSongTime() : previousSongTime;
    public BeatData LoadedBeatData => recordedBeatData;
    public IReadOnlyList<BeatEvent> BeatEvents => beatEvents;

    private BeatData recordedBeatData;
    private List<BeatEvent> beatEvents;
    public int currentBeatIndex;
    private float previousSongTime;
    private bool wasPlaying;
    private double dspSongStartTime;
    private float dspSongOffset;
    private bool dspClockInitialized;
    public float songtime;
    private int loopCount;

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
        songtime = CurrentSongTime;
    }

    public void LoadPreRecordedData()
    {
        BeatData previousData = recordedBeatData;
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

        loopCount = 0;
        SongChanged?.Invoke(previousData, recordedBeatData);
        enabled = true;
    }

    public bool LoadRuntimeBeatData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        BeatData data = JsonUtility.FromJson<BeatData>(json);
        return LoadRuntimeBeatData(data);
    }

    public bool LoadRuntimeBeatData(BeatData data)
    {
        if (data == null || data.beatEvents == null)
            return false;

        BeatData previousData = recordedBeatData;
        currentBeatIndex = 0;
        previousSongTime = 0f;
        recordedBeatData = data;
        beatEvents = recordedBeatData.beatEvents;
        beatEvents.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
        loopCount = 0;
        SongChanged?.Invoke(previousData, recordedBeatData);
        enabled = true;
        return true;
    }

    public bool SetRuntimeSong(AudioClip clip, BeatData beatData)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || clip == null || beatData == null || beatData.beatEvents == null)
            return false;

        audioSource.clip = clip;
        return LoadRuntimeBeatData(beatData);
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
        InitializeDspClock(0f);
        audioSource.Play();
    }

    public void StopPlayback()
    {
        if (audioSource != null)
            audioSource.Stop();

        ResetPlaybackState(0f);
        dspClockInitialized = false;
    }

    public void ResetPlaybackState(float songTime)
    {
        #if SOFTCEN_DEBUG
        Debug.Log("BeatPlay ResetPlaybackState");
        #endif

        currentBeatIndex = FindFirstBeatIndexAtOrAfter(songTime);
        previousSongTime = songTime;
        wasPlaying = false;
        if (useDspClock)
            InitializeDspClock(songTime);
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

        float reportedSongTime = GetReportedAudioTime();
        if (!wasPlaying)
            InitializeDspClock(reportedSongTime);

        float songTime = GetSongTime();
        RebaseDspClockIfNeeded(songTime, reportedSongTime);
        songTime = GetSongTime();
        float triggerTime = songTime + visualLeadTime;

        // Detect loop, rewind, or manual seek backwards.
        if (wasPlaying && songTime + 0.05f < previousSongTime)
        {
            bool consideredLoop = audioSource.loop && previousSongTime > 0.2f;
            if (consideredLoop)
            {
                loopCount++;
                SongLooped?.Invoke(loopCount, previousSongTime, songTime);
            }
            else
            {
                SongSeeked?.Invoke(previousSongTime, songTime);
            }

            ResetPlaybackState(songTime);
        }

        // Detect large forward seeks so old missed events are not fired in one burst.
        // Small frame-to-frame movement is handled normally.
        if (wasPlaying && songTime - previousSongTime > 1.0f)
        {
            SongSeeked?.Invoke(previousSongTime, songTime);
            currentBeatIndex = FindFirstBeatIndexAtOrAfter(previousSongTime);
        }

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
        if (useDspClock && audioSource != null && audioSource.isPlaying && dspClockInitialized)
        {
            float elapsed = (float)(AudioSettings.dspTime - dspSongStartTime);
            float songTime = dspSongOffset + elapsed;

            if (audioSource.loop && audioSource.clip != null && audioSource.clip.length > 0f)
                songTime = Mathf.Repeat(songTime, audioSource.clip.length);

            return songTime;
        }

        return GetReportedAudioTime();
    }

    private float GetReportedAudioTime()
    {
        if (useTimeSamples && audioSource.clip != null && audioSource.clip.frequency > 0)
            return audioSource.timeSamples / (float)audioSource.clip.frequency;

        return audioSource.time;
    }

    private void InitializeDspClock(float songTime)
    {
        dspSongStartTime = AudioSettings.dspTime;
        dspSongOffset = songTime;
        dspClockInitialized = true;
    }

    private void RebaseDspClockIfNeeded(float dspSongTime, float reportedSongTime)
    {
        if (!useDspClock || !dspClockInitialized)
            return;

        if (Mathf.Abs(dspSongTime - reportedSongTime) > 0.08f)
            InitializeDspClock(reportedSongTime);
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
