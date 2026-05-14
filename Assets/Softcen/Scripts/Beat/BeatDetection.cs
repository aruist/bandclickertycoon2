using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class BeatData
{
    public int analysisVersion = 1;
    public string analyzerId = "BeatDetection";
    public string clipName;
    public float audioLength;
    public int clipFrequency;
    public int outputSampleRate;
    public int fftSize;
    public int historyLength;
    public float minBeatSeparation;
    public List<BeatEvent> beatEvents = new List<BeatEvent>();
    public List<HypeChange> hypeEvents = new List<HypeChange>();
}

[Serializable]
public class BeatEvent
{
    public BeatDetection.BeatType beatType;
    public float timestamp;
    public float intensity;
}

public enum HypeState
{
    Low,
    Medium,
    High
}

[Serializable]
public class HypeChange
{
    public float timestamp;
    public HypeState newState;
    public bool isMoshZone;
}

public class BeatDetection : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AudioSource audioSource;

    public enum BeatMode { Energy, Frequency, Both }

    [Flags]
    public enum BeatType
    {
        None = 0,
        Kick = 1,
        Snare = 2,
        HiHat = 4,
        Bass = 8,
        Mid = 16,
        High = 32,
        Energy = 64,
        BassDrum = 128,
        Tom = 256,
        Cymbal = 512
    }

    public enum EventType
    {
        Energy, Kick, Snare, HiHat, Bass, Mid, High, BassDrum, Tom, Cymbal
    }

    [Serializable]
    public class BeatDetectionSettings
    {
        [Header("General")]
        public BeatMode beatMode = BeatMode.Both;
        [Min(64)] public int numSamples = 2048;
        [Min(1)] public int historyLength = 43;
        [Min(1)] public int minHistoryBeforeDetection = 8;
        [Min(0.01f)] public float minBeatSeparation = 0.06f;

        [Header("Sensitivity")]
        [Tooltip("Higher value = fewer global energy beats.")]
        [Range(1f, 4f)] public float energySensitivity = 1.5f;

        [Tooltip("Higher value = fewer frequency-band beats.")]
        [Range(1f, 4f)] public float frequencySensitivity = 1.8f;

        [Tooltip("Minimum RMS energy before an Energy beat can be emitted.")]
        [Min(0f)] public float minEnergyThreshold = 0.001f;

        [Tooltip("Minimum frequency-band value before a band beat can be emitted.")]
        [Min(0f)] public float minFrequencyThreshold = 0.00001f;

        [Tooltip("Used to normalize intensity. Larger value gives softer intensity values.")]
        [Min(0.01f)] public float intensityNormalizeRange = 2f;

        [Header("Output")]
        public string resourcesSubFolder = "BeatData";
        public string filePrefix = "BeatData_";
        public bool roundTimestamps = true;
        [Range(1, 5)] public int timestampDecimals = 3;
        public bool roundIntensities = true;
        [Range(1, 5)] public int intensityDecimals = 3;

        [Header("Preview Events")]
        public bool invokeCallbackWhileRecording = false;

        [Header("Frequency Ranges")]
        public FrequencyRange[] frequencyRanges = new FrequencyRange[]
        {
            new FrequencyRange(30f, 80f, BeatType.BassDrum), // Captures the physical "weight" of the sub. Original: 20-60
            new FrequencyRange(40f, 150f, BeatType.Kick), // Includes the fundamental "thump" of most kicks. Original: 60-120
            new FrequencyRange(80f, 400f, BeatType.Bass), // Bass guitars and synths often extend into the low-mids. Original: 120-250
            new FrequencyRange(1000f, 3000f, BeatType.Snare), // Focuses on the "crack" transient for better detection. Original: 250-500
            new FrequencyRange(500f, 1000f, BeatType.Tom),
            new FrequencyRange(1000f, 3000f, BeatType.Mid),
            new FrequencyRange(3000f, 8000f, BeatType.HiHat),
            new FrequencyRange(8000f, 16000f, BeatType.Cymbal)
        };
    }

    [Serializable]
    public class FrequencyRange
    {
        public float lowFreq;
        public float highFreq;
        public BeatType beatType;

        public FrequencyRange(float low, float high, BeatType type)
        {
            lowFreq = low;
            highFreq = high;
            beatType = type;
        }
    }

    public class EventInfo
    {
        public EventType messageInfo;
        public BeatDetection sender;
        public float intensity;
        public BeatType beatType;
    }

    public delegate void CallbackEventHandler(EventInfo eventInfo);
    public CallbackEventHandler CallBackFunction;

    public BeatDetectionSettings settings = new BeatDetectionSettings();

    private const int MaxFreqBands = 32;
    private const float Epsilon = 0.0000001f;

    private static readonly BeatType[] OutputBeatTypes =
    {
        BeatType.Kick,
        BeatType.Snare,
        BeatType.HiHat,
        BeatType.Bass,
        BeatType.Mid,
        BeatType.High,
        BeatType.Energy,
        BeatType.BassDrum,
        BeatType.Tom,
        BeatType.Cymbal
    };

    private float[] spectrumLeft;
    private float[] spectrumRight;
    private float[] samplesLeft;
    private float[] samplesRight;

    private float[] energyHistory;
    private float[,] freqBandHistory;
    private float[] currentFreqBands;
    private float[] currentBandThresholds;
    private float currentEnergy;
    private float currentEnergyThreshold;

    private readonly Dictionary<BeatType, float> lastBeatTimeByType = new Dictionary<BeatType, float>();

    private int historyCount;
    private int historyIndex;
    private int totalFreqBands;
    private int clipFrequency;
    private bool recordingStarted;
    private bool dataSaved;

    private BeatData recordedBeatData;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogError("BeatDetection needs an AudioSource with an AudioClip.", this);
            enabled = false;
            return;
        }

        PrepareRecording();

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    private void Update()
    {
        if (!recordingStarted || dataSaved)
            return;

        if (audioSource.isPlaying)
        {
            DetectAndRecordFrame();
            return;
        }

        SaveBeatData();
        Debug.Log($"Beat recording complete. Recorded {recordedBeatData.beatEvents.Count} events from {audioSource.clip.name}.", this);
        enabled = false;
    }

    private void PrepareRecording()
    {
        Debug.Log("PrepareRecording...");
        settings.numSamples = Mathf.NextPowerOfTwo(Mathf.Max(64, settings.numSamples));
        settings.historyLength = Mathf.Max(1, settings.historyLength);
        settings.minHistoryBeforeDetection = Mathf.Clamp(settings.minHistoryBeforeDetection, 1, settings.historyLength);

        spectrumLeft = new float[settings.numSamples];
        spectrumRight = new float[settings.numSamples];
        samplesLeft = new float[settings.numSamples];
        samplesRight = new float[settings.numSamples];

        totalFreqBands = Mathf.Min(settings.frequencyRanges != null ? settings.frequencyRanges.Length : 0, MaxFreqBands);
        currentFreqBands = new float[totalFreqBands];
        currentBandThresholds = new float[totalFreqBands];
        energyHistory = new float[settings.historyLength];
        freqBandHistory = new float[totalFreqBands, settings.historyLength];

        clipFrequency = audioSource.clip.frequency;
        historyCount = 0;
        historyIndex = 0;
        dataSaved = false;
        recordingStarted = true;
        lastBeatTimeByType.Clear();

        foreach (BeatType beatType in OutputBeatTypes)
            lastBeatTimeByType[beatType] = -999f;

        recordedBeatData = new BeatData
        {
            clipName = audioSource.clip.name,
            audioLength = audioSource.clip.length,
            clipFrequency = audioSource.clip.frequency,
            outputSampleRate = AudioSettings.outputSampleRate,
            fftSize = settings.numSamples,
            historyLength = settings.historyLength,
            minBeatSeparation = settings.minBeatSeparation,
            beatEvents = new List<BeatEvent>()
        };
    }

    private void DetectAndRecordFrame()
    {
        audioSource.GetSpectrumData(spectrumLeft, 0, FFTWindow.BlackmanHarris);
        audioSource.GetSpectrumData(spectrumRight, 1, FFTWindow.BlackmanHarris);
        audioSource.GetOutputData(samplesLeft, 0);
        audioSource.GetOutputData(samplesRight, 1);

        currentEnergy = CalculateRmsEnergy();
        CalculateFrequencyBands();

        float songTime = GetAccurateSongTime();
        List<DetectedBeat> detectedBeats = DetectBeats(songTime);

        for (int i = 0; i < detectedBeats.Count; i++)
        {
            DetectedBeat detectedBeat = detectedBeats[i];
            RecordBeatEvent(detectedBeat.beatType, songTime, detectedBeat.intensity);

            if (settings.invokeCallbackWhileRecording)
            {
                CallBackFunction?.Invoke(new EventInfo
                {
                    sender = this,
                    messageInfo = ConvertToEventType(detectedBeat.beatType),
                    intensity = detectedBeat.intensity,
                    beatType = detectedBeat.beatType
                });
            }
        }

        StoreCurrentFrameInHistory();
    }

    private List<DetectedBeat> DetectBeats(float songTime)
    {
        List<DetectedBeat> detected = new List<DetectedBeat>(4);

        if (historyCount < settings.minHistoryBeforeDetection)
            return detected;

        if (settings.beatMode == BeatMode.Energy || settings.beatMode == BeatMode.Both)
        {
            if (TryDetectEnergyBeat(songTime, out float intensity))
                detected.Add(new DetectedBeat(BeatType.Energy, intensity));
        }

        if (settings.beatMode == BeatMode.Frequency || settings.beatMode == BeatMode.Both)
        {
            for (int i = 0; i < totalFreqBands; i++)
            {
                if (TryDetectBandBeat(i, songTime, out float intensity))
                    detected.Add(new DetectedBeat(settings.frequencyRanges[i].beatType, intensity));
            }
        }

        return detected;
    }

    private bool TryDetectEnergyBeat(float songTime, out float intensity)
    {
        float average = CalculateAverage(energyHistory, historyCount);
        float variance = CalculateVariance(energyHistory, average, historyCount);
        currentEnergyThreshold = settings.energySensitivity * (average + variance * 0.5f);

        bool isBeat = currentEnergy > currentEnergyThreshold &&
                      currentEnergy > settings.minEnergyThreshold &&
                      songTime - lastBeatTimeByType[BeatType.Energy] >= settings.minBeatSeparation;

        intensity = isBeat ? CalculateIntensity(currentEnergy, currentEnergyThreshold) : 0f;

        if (isBeat)
            lastBeatTimeByType[BeatType.Energy] = songTime;

        return isBeat;
    }

    private bool TryDetectBandBeat(int bandIndex, float songTime, out float intensity)
    {
        BeatType beatType = settings.frequencyRanges[bandIndex].beatType;
        float currentValue = currentFreqBands[bandIndex];
        float average = CalculateBandAverage(bandIndex);
        float variance = CalculateBandVariance(bandIndex, average);
        float threshold = settings.frequencySensitivity * (average + variance * 0.3f);
        currentBandThresholds[bandIndex] = threshold;

        bool isBeat = currentValue > threshold &&
                      currentValue > settings.minFrequencyThreshold &&
                      songTime - lastBeatTimeByType[beatType] >= settings.minBeatSeparation;

        intensity = isBeat ? CalculateIntensity(currentValue, threshold) : 0f;

        if (isBeat)
            lastBeatTimeByType[beatType] = songTime;

        return isBeat;
    }

    private float CalculateRmsEnergy()
    {
        float energy = 0f;

        for (int i = 0; i < settings.numSamples; i++)
            energy += samplesLeft[i] * samplesLeft[i] + samplesRight[i] * samplesRight[i];

        return Mathf.Sqrt(energy / (settings.numSamples * 2f));
    }

    private void CalculateFrequencyBands()
    {
        for (int i = 0; i < totalFreqBands; i++)
        {
            FrequencyRange range = settings.frequencyRanges[i];
            currentFreqBands[i] = CalculateFrequencyRangeEnergy(range.lowFreq, range.highFreq);
        }
    }

    private float CalculateFrequencyRangeEnergy(float lowFreq, float highFreq)
    {
        int lowIndex = FrequencyToIndex(lowFreq);
        int highIndex = FrequencyToIndex(highFreq);

        if (highIndex < lowIndex)
            (lowIndex, highIndex) = (highIndex, lowIndex);

        float sum = 0f;
        int count = 0;

        for (int i = lowIndex; i <= highIndex && i < spectrumLeft.Length; i++)
        {
            sum += Mathf.Max(spectrumLeft[i], spectrumRight[i]);
            count++;
        }

        return count > 0 ? sum / count : 0f;
    }

    private int FrequencyToIndex(float frequency)
    {
        // Unity spectrum bins cover 0 Hz to Nyquist frequency, so use 2 * frequency / sampleRate.
        int index = Mathf.RoundToInt(frequency * settings.numSamples * 2f / clipFrequency);
        return Mathf.Clamp(index, 0, settings.numSamples - 1);
    }

    private void StoreCurrentFrameInHistory()
    {
        energyHistory[historyIndex] = currentEnergy;

        for (int i = 0; i < totalFreqBands; i++)
            freqBandHistory[i, historyIndex] = currentFreqBands[i];

        historyCount = Mathf.Min(historyCount + 1, settings.historyLength);
        historyIndex = (historyIndex + 1) % settings.historyLength;
    }

    private float CalculateAverage(float[] history, int count)
    {
        if (count <= 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < count; i++)
            sum += history[i];

        return sum / count;
    }

    private float CalculateVariance(float[] history, float average, int count)
    {
        if (count <= 1)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            float diff = history[i] - average;
            sum += diff * diff;
        }

        return sum / (count - 1);
    }

    private float CalculateBandAverage(int bandIndex)
    {
        if (historyCount <= 0)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < historyCount; i++)
            sum += freqBandHistory[bandIndex, i];

        return sum / historyCount;
    }

    private float CalculateBandVariance(int bandIndex, float average)
    {
        if (historyCount <= 1)
            return 0f;

        float sum = 0f;
        for (int i = 0; i < historyCount; i++)
        {
            float diff = freqBandHistory[bandIndex, i] - average;
            sum += diff * diff;
        }

        return sum / (historyCount - 1);
    }

    private float CalculateIntensity(float currentValue, float threshold)
    {
        if (threshold <= Epsilon)
            return 1f;

        float overThreshold = Mathf.Max(0f, currentValue - threshold);
        float normalized = overThreshold / Mathf.Max(Epsilon, threshold * settings.intensityNormalizeRange);
        return Mathf.Clamp01(0.25f + normalized * 0.75f);
    }

    private float GetAccurateSongTime()
    {
        if (audioSource.clip == null || audioSource.clip.frequency <= 0)
            return audioSource.time;

        return audioSource.timeSamples / (float)audioSource.clip.frequency;
    }

    private void RecordBeatEvent(BeatType beatType, float timestamp, float intensity)
    {
        recordedBeatData.beatEvents.Add(new BeatEvent
        {
            beatType = beatType,
            timestamp = settings.roundTimestamps ? Round(timestamp, settings.timestampDecimals) : timestamp,
            intensity = settings.roundIntensities ? Round(intensity, settings.intensityDecimals) : intensity
        });
    }

    private void SaveBeatData()
    {
        if (dataSaved || recordedBeatData == null || audioSource == null || audioSource.clip == null)
            return;

        dataSaved = true;

#if UNITY_EDITOR
        string resourcesPath = Path.Combine(Application.dataPath, "Resources");
        string targetFolder = string.IsNullOrWhiteSpace(settings.resourcesSubFolder)
            ? resourcesPath
            : Path.Combine(resourcesPath, settings.resourcesSubFolder);

        Directory.CreateDirectory(targetFolder);

        string safeClipName = MakeSafeFileName(audioSource.clip.name);
        string fileName = $"{settings.filePrefix}{safeClipName}.json";
        string filePath = Path.Combine(targetFolder, fileName);

        string jsonData = JsonUtility.ToJson(recordedBeatData, true);
        File.WriteAllText(filePath, jsonData);

        AssetDatabase.Refresh();
        Debug.Log($"Beat data saved to: {filePath}", this);
#else
        Debug.LogWarning("BeatDetection JSON generation is intended for Editor use only. No file was saved in this build.", this);
#endif
    }

    private static string MakeSafeFileName(string fileName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalidChar, '_');

        return fileName.Replace(' ', '_');
    }

    private static float Round(float value, int decimals)
    {
        return (float)Math.Round(value, Mathf.Clamp(decimals, 0, 5));
    }

    private EventType ConvertToEventType(BeatType beatType)
    {
        return beatType switch
        {
            BeatType.Kick => EventType.Kick,
            BeatType.Snare => EventType.Snare,
            BeatType.HiHat => EventType.HiHat,
            BeatType.Bass => EventType.Bass,
            BeatType.Mid => EventType.Mid,
            BeatType.High => EventType.High,
            BeatType.Energy => EventType.Energy,
            BeatType.BassDrum => EventType.BassDrum,
            BeatType.Tom => EventType.Tom,
            BeatType.Cymbal => EventType.Cymbal,
            _ => EventType.Energy
        };
    }

    public void SetSensitivity(float energySensitivity, float frequencySensitivity)
    {
        settings.energySensitivity = Mathf.Clamp(energySensitivity, 1f, 4f);
        settings.frequencySensitivity = Mathf.Clamp(frequencySensitivity, 1f, 4f);
    }

    public float GetCurrentEnergy() => currentEnergy;

    public float GetCurrentEnergyThreshold() => currentEnergyThreshold;

    public float GetFrequencyBandValue(int bandIndex)
    {
        return bandIndex >= 0 && bandIndex < totalFreqBands ? currentFreqBands[bandIndex] : 0f;
    }

    public float GetFrequencyBandThreshold(int bandIndex)
    {
        return bandIndex >= 0 && bandIndex < totalFreqBands ? currentBandThresholds[bandIndex] : 0f;
    }

    public int GetTotalFrequencyBands() => totalFreqBands;

    private readonly struct DetectedBeat
    {
        public readonly BeatType beatType;
        public readonly float intensity;

        public DetectedBeat(BeatType beatType, float intensity)
        {
            this.beatType = beatType;
            this.intensity = intensity;
        }
    }
}
