#if NOT_USED
using UnityEngine;
using System;
using System.Collections.Generic;
[System.Serializable]
public class BeatData
{
    public List<BeatEvent> beatEvents;
    public float audioLength;
    public int sampleRate;
    public int fftSize;
}

[System.Serializable]
public class BeatEvent
{
    public BeatDetection.BeatType beatType;
    public float timestamp;
    public float intensity;
}

public class BeatDetection : MonoBehaviour
{
    public AudioSource audioSource;
    public enum BeatMode { Energy, Frequency, Both };
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
        [Header("General Settings")]
        public BeatMode beatMode = BeatMode.Both;
        public int numSamples = 2048; // Increased for better frequency resolution
        public float minBeatSeparation = 0.05f;

        [Header("Sensitivity Settings")]
        [Range(1f, 3f)] public float energySensitivity = 1.5f;
        [Range(1f, 3f)] public float frequencySensitivity = 1.8f;

        [Header("Frequency Ranges")]
        public FrequencyRange[] frequencyRanges = new FrequencyRange[]
        {
            new FrequencyRange(20f, 60f, BeatType.BassDrum),    // Sub-bass
            new FrequencyRange(60f, 120f, BeatType.Kick),       // Kick drum
            new FrequencyRange(120f, 250f, BeatType.Bass),      // Bass
            new FrequencyRange(250f, 500f, BeatType.Snare),     // Snare body
            new FrequencyRange(500f, 1000f, BeatType.Tom),      // Toms
            new FrequencyRange(1000f, 3000f, BeatType.Mid),     // Mid range
            new FrequencyRange(3000f, 8000f, BeatType.HiHat),   // Hi-hats
            new FrequencyRange(8000f, 16000f, BeatType.Cymbal)  // Cymbals
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

    // Constants
    private const int HISTORY_LENGTH = 43;
    private const int MAX_FREQ_BANDS = 32;
    private const float MIN_ENERGY_THRESHOLD = 0.001f;

    // Private variables
    private int numHistory, circularHistory;
    private float sampleRate;
    private float tIni;

    // History buffers
    private float[] energyHistory = new float[HISTORY_LENGTH];
    private float[] varianceHistory = new float[HISTORY_LENGTH];
    private float[,] freqBandHistory = new float[MAX_FREQ_BANDS, HISTORY_LENGTH];
    private float[,] freqVarianceHistory = new float[MAX_FREQ_BANDS, HISTORY_LENGTH];

    // Current values
    private float[] currentFreqBands = new float[MAX_FREQ_BANDS];
    private float currentEnergy;

    // Spectrum data
    private float[] spectrum0;
    private float[] spectrum1;
    private float[] frames0;
    private float[] frames1;

    // Frequency band management
    private int totalFreqBands;
    private float[] lastBeatTime = new float[MAX_FREQ_BANDS];

    private BeatData recordedBeatData;

    void Start()
    {
        spectrum0 = new float[settings.numSamples];
        spectrum1 = new float[settings.numSamples];
        frames0 = new float[settings.numSamples];
        frames1 = new float[settings.numSamples];

        sampleRate = AudioSettings.outputSampleRate;
        InitializeFrequencyBands();
        InitializeHistory();

        tIni = Time.time;
        for (int i = 0; i < MAX_FREQ_BANDS; i++)
            lastBeatTime[i] = Time.time;

        StartPreRecording();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            int detectedBeats = DetectBeats();

            // Send events for each detected beat type
            foreach (BeatType beatType in Enum.GetValues(typeof(BeatType)))
            {
                if (beatType == BeatType.None) continue;

                if ((detectedBeats & (int)beatType) != 0)
                {
                    SendEvent(ConvertToEventType(beatType), GetBeatIntensity(beatType), beatType);
                }
            }
        }
        else {
            SaveBeatData();
            Debug.Log($"Beat recording complete! Recorded {recordedBeatData.beatEvents.Count} events");
            this.enabled = false;
        }
    }
    private void StartPreRecording()
    {
        recordedBeatData = new BeatData();
        recordedBeatData.beatEvents = new List<BeatEvent>();
        recordedBeatData.sampleRate = AudioSettings.outputSampleRate;
        recordedBeatData.fftSize = settings.numSamples;

        // Start recording coroutine
        //StartCoroutine(RecordBeatData());
    }

    private int DetectBeats()
    {
        // Get audio data
        audioSource.GetSpectrumData(spectrum0, 0, FFTWindow.BlackmanHarris);
        audioSource.GetSpectrumData(spectrum1, 1, FFTWindow.BlackmanHarris);
        audioSource.GetOutputData(frames0, 0);
        audioSource.GetOutputData(frames1, 1);
        // GetComponent<AudioSource>().GetSpectrumData(spectrum0, 0, FFTWindow.BlackmanHarris);
        // GetComponent<AudioSource>().GetSpectrumData(spectrum1, 1, FFTWindow.BlackmanHarris);
        // GetComponent<AudioSource>().GetOutputData(frames0, 0);
        // GetComponent<AudioSource>().GetOutputData(frames1, 1);

        int detectedBeats = 0;

        switch (settings.beatMode)
        {
            case BeatMode.Energy:
                if (DetectEnergyBeat())
                    detectedBeats |= (int)BeatType.Energy;
                break;

            case BeatMode.Frequency:
                detectedBeats = DetectFrequencyBeats();
                break;

            case BeatMode.Both:
                if (DetectEnergyBeat())
                    detectedBeats |= (int)BeatType.Energy;
                detectedBeats |= DetectFrequencyBeats();
                break;
        }

        return detectedBeats;
    }

    private void InitializeFrequencyBands()
    {
        totalFreqBands = Mathf.Min(settings.frequencyRanges.Length, MAX_FREQ_BANDS);
    }

    private void InitializeHistory()
    {
        numHistory = 0;
        circularHistory = 0;

        for (int i = 0; i < HISTORY_LENGTH; i++)
        {
            energyHistory[i] = 0f;
            varianceHistory[i] = 0f;
        }

        for (int i = 0; i < totalFreqBands; i++)
        {
            for (int j = 0; j < HISTORY_LENGTH; j++)
            {
                freqBandHistory[i, j] = 0f;
                freqVarianceHistory[i, j] = 0f;
            }
        }
    }

    private bool DetectEnergyBeat()
    {
        // Calculate current energy
        currentEnergy = CalculateEnergy();

        // Update history
        UpdateEnergyHistory();

        // Calculate statistics
        float averageEnergy = CalculateAverage(energyHistory, numHistory);
        float energyVariance = CalculateVariance(energyHistory, averageEnergy, numHistory);

        // Dynamic threshold based on variance
        float threshold = settings.energySensitivity * (averageEnergy + energyVariance * 0.5f);

        // Check for beat with temporal constraints
        bool isBeat = currentEnergy > threshold &&
                     currentEnergy > MIN_ENERGY_THRESHOLD &&
                     (Time.time - tIni) > settings.minBeatSeparation;

        if (isBeat)
        {
            tIni = Time.time;
        }

        return isBeat;
    }

    private int DetectFrequencyBeats()
    {
        int detectedBeats = 0;

        // Calculate frequency bands
        CalculateFrequencyBands();

        for (int i = 0; i < totalFreqBands; i++)
        {
            if (DetectBandBeat(i))
            {
                detectedBeats |= (int)settings.frequencyRanges[i].beatType;
                lastBeatTime[i] = Time.time;
            }
        }

        return detectedBeats;
    }

    private bool DetectBandBeat(int bandIndex)
    {
        float currentValue = currentFreqBands[bandIndex];

        // Update history
        UpdateBandHistory(bandIndex);

        // Calculate statistics
        float average = CalculateBandAverage(bandIndex);
        float variance = CalculateBandVariance(bandIndex, average);

        // Dynamic threshold with sensitivity adjustment
        float threshold = settings.frequencySensitivity * (average + variance * 0.3f);

        // Check for beat
        bool isBeat = currentValue > threshold &&
                     (Time.time - lastBeatTime[bandIndex]) > settings.minBeatSeparation;

        return isBeat;
    }

    private float CalculateEnergy()
    {
        float energy = 0f;
        for (int i = 0; i < settings.numSamples; i++)
        {
            energy += (frames0[i] * frames0[i]) + (frames1[i] * frames1[i]);
        }
        return Mathf.Sqrt(energy / settings.numSamples) * 100f;
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

        if (lowIndex == highIndex) return spectrum0[lowIndex] + spectrum1[lowIndex];

        float energy = 0f;
        for (int i = lowIndex; i <= highIndex && i < spectrum0.Length; i++)
        {
            energy += Mathf.Max(spectrum0[i], spectrum1[i]);
        }

        return energy / (highIndex - lowIndex + 1);
    }

    private int FrequencyToIndex(float frequency)
    {
        return Mathf.FloorToInt(frequency * settings.numSamples / sampleRate);
    }

    private void UpdateEnergyHistory()
    {
        energyHistory[circularHistory] = currentEnergy;
        numHistory = Mathf.Min(numHistory + 1, HISTORY_LENGTH);
        circularHistory = (circularHistory + 1) % HISTORY_LENGTH;
    }

    private void UpdateBandHistory(int bandIndex)
    {
        freqBandHistory[bandIndex, circularHistory] = currentFreqBands[bandIndex];
    }

    private float CalculateAverage(float[] history, int count)
    {
        if (count == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            sum += history[i];
        }
        return sum / count;
    }

    private float CalculateVariance(float[] history, float average, int count)
    {
        if (count <= 1) return 0f;

        float variance = 0f;
        for (int i = 0; i < count; i++)
        {
            float diff = history[i] - average;
            variance += diff * diff;
        }
        return variance / (count - 1);
    }

    private float CalculateBandAverage(int bandIndex)
    {
        float sum = 0f;
        for (int i = 0; i < numHistory; i++)
        {
            sum += freqBandHistory[bandIndex, i];
        }
        return numHistory > 0 ? sum / numHistory : 0f;
    }

    private float CalculateBandVariance(int bandIndex, float average)
    {
        if (numHistory <= 1) return 0f;

        float variance = 0f;
        for (int i = 0; i < numHistory; i++)
        {
            float diff = freqBandHistory[bandIndex, i] - average;
            variance += diff * diff;
        }
        return variance / (numHistory - 1);
    }

    private float GetBeatIntensity(BeatType beatType)
    {
        // Calculate intensity based on how much the current value exceeds the threshold
        // This is a simplified version - you can expand this based on your needs
        return 1.0f; // Placeholder
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

    private void SendEvent(EventType eventType, float intensity, BeatType beatType)
    {
        //Debug.Log($"eventType: {eventType}, beatType: {beatType}, intensity: {intensity}, time: {audioSource.time}");
        RecordBeatEvent(beatType, audioSource.time, intensity);
        // CallBackFunction?.Invoke(new EventInfo
        // {
        //     sender = this,
        //     messageInfo = eventType,
        //     intensity = intensity,
        //     beatType = beatType
        // });
    }

    private void SaveBeatData()
    {
        string jsonData = JsonUtility.ToJson(recordedBeatData, true);
        string filePath = Application.dataPath + $"/Resources/BeatData_{audioSource.clip.name}.json";
        System.IO.File.WriteAllText(filePath, jsonData);
        Debug.Log($"Beat data saved to: {filePath}");
    }

    private void RecordBeatEvent(BeatType beatType, float timestamp, float intensity)
    {
        recordedBeatData.beatEvents.Add(new BeatEvent
        {
            beatType = beatType,
            timestamp = timestamp,
            intensity = intensity
        });
    }

    // Public methods for external control
    public void SetSensitivity(float energySens, float freqSens)
    {
        settings.energySensitivity = Mathf.Clamp(energySens, 1f, 3f);
        settings.frequencySensitivity = Mathf.Clamp(freqSens, 1f, 3f);
    }

    public float GetCurrentEnergy()
    {
        return currentEnergy;
    }

    public float GetFrequencyBandValue(int bandIndex)
    {
        if (bandIndex >= 0 && bandIndex < totalFreqBands)
            return currentFreqBands[bandIndex];
        return 0f;
    }

    public int GetTotalFrequencyBands()
    {
        return totalFreqBands;
    }
}
#endif