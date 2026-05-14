using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Offline PCM beat analyzer for user-imported songs.
/// MP3 decoding and AudioClip.GetData stay on Unity's main thread; window math runs in a background Task.
/// </summary>
public sealed class PCMBeatDetection : MonoBehaviour
{
    public const int AnalysisVersion = 2;
    public const string AnalyzerId = "PCMBeatDetection";
    [SerializeField] private BeatDetection.BeatDetectionSettings settings = new BeatDetection.BeatDetectionSettings();
    [SerializeField, Min(0)] private int hopSize = 512;
    [SerializeField] private bool prettyPrintJson = true;
    [SerializeField] private bool destroyDecodedClipAfterAnalysis = true;
    [Header("Hype Analysis")]
    [SerializeField, Min(0.1f)] private float hypeLookAheadSeconds = 3f;
    [SerializeField, Min(0.1f)] private float hypeSampleStepSeconds = 0.25f;
    [SerializeField, Min(0)] private int quietWindowBeatThreshold = 8;
    [SerializeField, Min(0f)] private float mediumBeatsPerSecond = 1.2f;
    [SerializeField, Range(0f, 1f)] private float highAverageIntensity = 0.4f; //0.4 - 0.45
    [SerializeField, Min(1)] private int highUniqueBeatTypes = 3;
    [SerializeField, Min(0f)] private float simultaneousBeatWindow = 0.1f; // Recommended Value 0.08 - 0.12
    [SerializeField, Min(1)] private int moshUniqueBeatTypes = 3;
    [SerializeField, Min(0.2f)] private float hypeDropDetectionWindowSeconds = 2f;
    [SerializeField, Min(1f)] private float hypeDropRiseMultiplier = 1.25f;

    public float ProgressPercentage => progressPermille / 10f;
    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;
    public string ErrorMessage => errorMessage;
    public BeatData LastBeatData => lastBeatData;
    public string LastJson => lastJson;

    public event Action<float> ProgressChanged;
    public event Action<BeatData, string> AnalysisCompleted;
    public event Action<string> AnalysisFailed;

    private const int MaxFreqBands = 32;
    private const float Epsilon = 0.0000001f;

    private static readonly BeatDetection.BeatType[] OutputBeatTypes =
    {
        BeatDetection.BeatType.Kick,
        BeatDetection.BeatType.Snare,
        BeatDetection.BeatType.HiHat,
        BeatDetection.BeatType.Bass,
        BeatDetection.BeatType.Mid,
        BeatDetection.BeatType.High,
        BeatDetection.BeatType.Energy,
        BeatDetection.BeatType.BassDrum,
        BeatDetection.BeatType.Tom,
        BeatDetection.BeatType.Cymbal
    };

    private volatile bool isRunning;
    private volatile bool isComplete;
    private volatile bool cancelRequested;
    private volatile int progressPermille;

    private string errorMessage;
    private BeatData lastBeatData;
    private string lastJson;
    private Coroutine analysisCoroutine;

    public void AnalyzeMp3File(string mp3Path, string outputJsonPath = null)
    {
        if (isRunning)
        {
            Debug.LogWarning("PCM beat analysis is already running. Cancel it before starting another analysis.", this);
            return;
        }

        analysisCoroutine = StartCoroutine(AnalyzeMp3FileRoutine(mp3Path, outputJsonPath));
    }

    public void CancelAnalysis()
    {
        cancelRequested = true;
    }

    private void OnDestroy()
    {
        cancelRequested = true;
    }

    private IEnumerator AnalyzeMp3FileRoutine(string mp3Path, string outputJsonPath)
    {
        ResetState();

        if (string.IsNullOrWhiteSpace(mp3Path) || !File.Exists(mp3Path))
        {
            Fail($"MP3 file not found: {mp3Path}");
            yield break;
        }

        AudioClip decodedClip = null;
        float[] pcmSamples = null;

        try
        {
            SetProgress(0f);
            string fileUri = new Uri(Path.GetFullPath(mp3Path)).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Fail($"MP3 decode failed: {request.error}");
                    yield break;
                }

                decodedClip = DownloadHandlerAudioClip.GetContent(request);
            }

            if (decodedClip == null)
            {
                Fail("MP3 decode failed: decoded AudioClip was null.");
                yield break;
            }

            SetProgress(10f);

            int totalSamples = decodedClip.samples * decodedClip.channels;
            pcmSamples = new float[totalSamples];

            if (!decodedClip.GetData(pcmSamples, 0))
            {
                Fail("Could not read decoded PCM samples from AudioClip.");
                yield break;
            }

            AnalysisInput input = CreateAnalysisInput(decodedClip, mp3Path, pcmSamples);
            pcmSamples = null;

            if (destroyDecodedClipAfterAnalysis)
                Destroy(decodedClip);

            decodedClip = null;
            SetProgress(15f);

            Task<BeatData> analysisTask = Task.Run(() => AnalyzePcm(input));

            while (!analysisTask.IsCompleted)
            {
                SetProgress(15f + input.ProgressPermille / 1000f * 80f);
                yield return null;
            }

            if (analysisTask.IsFaulted)
            {
                string message = analysisTask.Exception != null
                    ? analysisTask.Exception.GetBaseException().Message
                    : "Unknown PCM analysis error.";
                Fail(message);
                yield break;
            }

            if (analysisTask.IsCanceled || cancelRequested)
            {
                Fail("PCM beat analysis was cancelled.");
                yield break;
            }

            lastBeatData = analysisTask.Result;
            lastJson = JsonUtility.ToJson(lastBeatData, prettyPrintJson);

            if (!string.IsNullOrWhiteSpace(outputJsonPath))
            {
                string directory = Path.GetDirectoryName(outputJsonPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(outputJsonPath, lastJson);
            }

            isComplete = true;
            isRunning = false;
            SetProgress(100f);
            AnalysisCompleted?.Invoke(lastBeatData, lastJson);
        }
        finally
        {
            if (pcmSamples != null)
                Array.Clear(pcmSamples, 0, pcmSamples.Length);

            if (decodedClip != null && destroyDecodedClipAfterAnalysis)
                Destroy(decodedClip);

            analysisCoroutine = null;
        }
    }

    private AnalysisInput CreateAnalysisInput(AudioClip clip, string mp3Path, float[] pcmSamples)
    {
        int fftSize = NextPowerOfTwo(Math.Max(64, settings.numSamples));
        int historyLength = Math.Max(1, settings.historyLength);
        int minHistory = Math.Max(1, Math.Min(settings.minHistoryBeforeDetection, historyLength));

        return new AnalysisInput
        {
            ClipName = Path.GetFileNameWithoutExtension(mp3Path),
            AudioLength = clip.length,
            ClipFrequency = clip.frequency,
            OutputSampleRate = AudioSettings.outputSampleRate,
            Channels = Math.Max(1, clip.channels),
            PcmSamples = pcmSamples,
            BeatMode = settings.beatMode,
            FftSize = fftSize,
            HopSize = Math.Max(1, hopSize > 0 ? hopSize : fftSize / 2),
            HistoryLength = historyLength,
            MinHistoryBeforeDetection = minHistory,
            MinBeatSeparation = Math.Max(0.01f, settings.minBeatSeparation),
            EnergySensitivity = settings.energySensitivity,
            FrequencySensitivity = settings.frequencySensitivity,
            MinEnergyThreshold = settings.minEnergyThreshold,
            MinFrequencyThreshold = settings.minFrequencyThreshold,
            IntensityNormalizeRange = Math.Max(0.01f, settings.intensityNormalizeRange),
            RoundTimestamps = settings.roundTimestamps,
            TimestampDecimals = Math.Max(0, Math.Min(5, settings.timestampDecimals)),
            RoundIntensities = settings.roundIntensities,
            IntensityDecimals = Math.Max(0, Math.Min(5, settings.intensityDecimals)),
            FrequencyRanges = CloneFrequencyRanges(settings.frequencyRanges),
            HypeLookAheadSeconds = Math.Max(0.1f, hypeLookAheadSeconds),
            HypeSampleStepSeconds = Math.Max(0.1f, hypeSampleStepSeconds),
            QuietWindowBeatThreshold = Math.Max(0, quietWindowBeatThreshold),
            MediumBeatsPerSecond = Math.Max(0f, mediumBeatsPerSecond),
            HighAverageIntensity = Mathf.Clamp01(highAverageIntensity),
            HighUniqueBeatTypes = Math.Max(1, highUniqueBeatTypes),
            SimultaneousBeatWindow = Math.Max(0f, simultaneousBeatWindow),
            MoshUniqueBeatTypes = Math.Max(1, moshUniqueBeatTypes),
            HypeDropDetectionWindowSeconds = Math.Max(0.2f, hypeDropDetectionWindowSeconds),
            HypeDropRiseMultiplier = Math.Max(1f, hypeDropRiseMultiplier)
        };
    }

    private static BeatDetection.FrequencyRange[] CloneFrequencyRanges(BeatDetection.FrequencyRange[] ranges)
    {
        if (ranges == null)
            return Array.Empty<BeatDetection.FrequencyRange>();

        int count = Math.Min(ranges.Length, MaxFreqBands);
        BeatDetection.FrequencyRange[] clone = new BeatDetection.FrequencyRange[count];

        for (int i = 0; i < count; i++)
            clone[i] = new BeatDetection.FrequencyRange(ranges[i].lowFreq, ranges[i].highFreq, ranges[i].beatType);

        return clone;
    }

    private BeatData AnalyzePcm(AnalysisInput input)
    {
        float[] pcm = input.PcmSamples;

        try
        {
            PcmAnalyzer analyzer = new PcmAnalyzer(input, () => cancelRequested);
            return analyzer.Analyze();
        }
        finally
        {
            if (pcm != null)
                Array.Clear(pcm, 0, pcm.Length);

            input.PcmSamples = null;
        }
    }

    private void ResetState()
    {
        cancelRequested = false;
        isRunning = true;
        isComplete = false;
        errorMessage = null;
        lastBeatData = null;
        lastJson = null;
        progressPermille = 0;
    }

    private void Fail(string message)
    {
        errorMessage = message;
        isRunning = false;
        isComplete = false;
        AnalysisFailed?.Invoke(message);
        Debug.LogError(message, this);
    }

    private void SetProgress(float percentage)
    {
        int nextPermille = Math.Max(0, Math.Min(1000, (int)Math.Round(percentage * 10f)));

        if (nextPermille == progressPermille)
            return;

        progressPermille = nextPermille;
        ProgressChanged?.Invoke(ProgressPercentage);
    }

    private static int NextPowerOfTwo(int value)
    {
        value = Math.Max(1, value);
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        return value;
    }

    private sealed class AnalysisInput
    {
        public string ClipName;
        public float AudioLength;
        public int ClipFrequency;
        public int OutputSampleRate;
        public int Channels;
        public float[] PcmSamples;
        public BeatDetection.BeatMode BeatMode;
        public int FftSize;
        public int HopSize;
        public int HistoryLength;
        public int MinHistoryBeforeDetection;
        public float MinBeatSeparation;
        public float EnergySensitivity;
        public float FrequencySensitivity;
        public float MinEnergyThreshold;
        public float MinFrequencyThreshold;
        public float IntensityNormalizeRange;
        public bool RoundTimestamps;
        public int TimestampDecimals;
        public bool RoundIntensities;
        public int IntensityDecimals;
        public BeatDetection.FrequencyRange[] FrequencyRanges;
        public float HypeLookAheadSeconds;
        public float HypeSampleStepSeconds;
        public int QuietWindowBeatThreshold;
        public float MediumBeatsPerSecond;
        public float HighAverageIntensity;
        public int HighUniqueBeatTypes;
        public float SimultaneousBeatWindow;
        public int MoshUniqueBeatTypes;
        public float HypeDropDetectionWindowSeconds;
        public float HypeDropRiseMultiplier;
        public volatile int ProgressPermille;
    }

    private sealed class PcmAnalyzer
    {
        private readonly AnalysisInput input;
        private readonly Func<bool> shouldCancel;
        private readonly int totalFrames;
        private readonly int totalWindows;
        private readonly int totalFreqBands;
        private readonly float[] energyHistory;
        private readonly float[,] freqBandHistory;
        private readonly float[] currentFreqBands;
        private readonly float[] previousFluxByBand;
        private readonly float[] previousPreviousFluxByBand;
        private readonly float[] previousThresholdByBand;
        private readonly float[] previousTimeByBand;
        private readonly bool[] hasPreviousFluxByBand;
        private readonly bool[] hasPreviousPreviousFluxByBand;
        private readonly Dictionary<BeatDetection.BeatType, float> lastBeatTimeByType = new Dictionary<BeatDetection.BeatType, float>();
        private readonly float[] leftReal;
        private readonly float[] leftImag;
        private readonly float[] rightReal;
        private readonly float[] rightImag;
        private readonly float[] window;
        private readonly float[] spectrumLeft;
        private readonly float[] spectrumRight;
        private readonly float[] previousSpectrumLeft;
        private readonly float[] previousSpectrumRight;
        private bool hasPreviousSpectrum;

        private int historyCount;
        private int historyIndex;
        private float currentEnergy;

        public PcmAnalyzer(AnalysisInput input, Func<bool> shouldCancel)
        {
            this.input = input;
            this.shouldCancel = shouldCancel;
            totalFrames = input.PcmSamples.Length / input.Channels;
            totalWindows = Math.Max(1, 1 + Math.Max(0, totalFrames - input.FftSize) / input.HopSize);
            totalFreqBands = Math.Min(input.FrequencyRanges != null ? input.FrequencyRanges.Length : 0, MaxFreqBands);
            energyHistory = new float[input.HistoryLength];
            freqBandHistory = new float[totalFreqBands, input.HistoryLength];
            currentFreqBands = new float[totalFreqBands];
            previousFluxByBand = new float[totalFreqBands];
            previousPreviousFluxByBand = new float[totalFreqBands];
            previousThresholdByBand = new float[totalFreqBands];
            previousTimeByBand = new float[totalFreqBands];
            hasPreviousFluxByBand = new bool[totalFreqBands];
            hasPreviousPreviousFluxByBand = new bool[totalFreqBands];

            leftReal = new float[input.FftSize];
            leftImag = new float[input.FftSize];
            rightReal = new float[input.FftSize];
            rightImag = new float[input.FftSize];
            spectrumLeft = new float[input.FftSize / 2];
            spectrumRight = new float[input.FftSize / 2];
            previousSpectrumLeft = new float[input.FftSize / 2];
            previousSpectrumRight = new float[input.FftSize / 2];
            window = CreateBlackmanHarrisWindow(input.FftSize);

            foreach (BeatDetection.BeatType beatType in OutputBeatTypes)
                lastBeatTimeByType[beatType] = -999f;
        }

        public BeatData Analyze()
        {
            BeatData beatData = new BeatData
            {
                analysisVersion = AnalysisVersion,
                analyzerId = AnalyzerId,
                clipName = input.ClipName,
                audioLength = input.AudioLength,
                clipFrequency = input.ClipFrequency,
                outputSampleRate = input.OutputSampleRate,
                fftSize = input.FftSize,
                historyLength = input.HistoryLength,
                minBeatSeparation = input.MinBeatSeparation,
                beatEvents = new List<BeatEvent>()
            };

            for (int windowIndex = 0; windowIndex < totalWindows; windowIndex++)
            {
                if (shouldCancel())
                    throw new OperationCanceledException();

                int frameOffset = windowIndex * input.HopSize;
                float songTime = frameOffset / (float)input.ClipFrequency;

                FillWindow(frameOffset);
                FastFourierTransform(leftReal, leftImag);
                FastFourierTransform(rightReal, rightImag);
                FillSpectrums();

                currentEnergy = CalculateRmsEnergy(frameOffset);
                CalculateFrequencyBands();
                DetectAndRecord(songTime, beatData.beatEvents);
                StoreCurrentFrameInHistory();
                StoreCurrentSpectrumAsPrevious();

                input.ProgressPermille = (int)Math.Round((windowIndex + 1) / (double)totalWindows * 1000.0);
            }

            input.ProgressPermille = 1000;
            beatData.hypeEvents = BuildHypeTimeline(beatData.beatEvents, input.AudioLength);
            return beatData;
        }

        private List<HypeChange> BuildHypeTimeline(List<BeatEvent> beatEvents, float audioLength)
        {
            List<HypeChange> timeline = new List<HypeChange>();
            if (beatEvents == null || beatEvents.Count == 0)
                return timeline;

            float duration = Math.Max(0.1f, audioLength);
            float step = Math.Max(0.1f, input.HypeSampleStepSeconds);
            HypeState? lastState = null;
            bool? lastMosh = null;

            for (float t = 0f; t <= duration + 0.001f; t += step)
            {
                HypeSample sample = AnalyzeWindowAtTime(beatEvents, t);
                if (!lastState.HasValue || sample.State != lastState.Value || sample.IsMoshZone != lastMosh.Value)
                {
                    timeline.Add(new HypeChange
                    {
                        timestamp = input.RoundTimestamps ? Round(t, input.TimestampDecimals) : t,
                        newState = sample.State,
                        isMoshZone = sample.IsMoshZone
                    });

                    lastState = sample.State;
                    lastMosh = sample.IsMoshZone;
                }
            }

            return timeline;
        }

        private HypeSample AnalyzeWindowAtTime(List<BeatEvent> beatEvents, float timestamp)
        {
            float lookAhead = Math.Max(0.1f, input.HypeLookAheadSeconds);
            float windowEnd = timestamp + lookAhead;
            float halfTime = timestamp + lookAhead * 0.5f;
            float dropWindow = Math.Min(lookAhead, Math.Max(0.2f, input.HypeDropDetectionWindowSeconds));
            float dropWindowStart = timestamp + Math.Max(0f, lookAhead - dropWindow);
            float dropHalfTime = dropWindowStart + dropWindow * 0.5f;

            int beatCount = 0;
            float intensitySum = 0f;
            float firstHalfIntensity = 0f;
            float secondHalfIntensity = 0f;
            float dropFirstHalfIntensity = 0f;
            float dropSecondHalfIntensity = 0f;
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

                if (beatEvent.timestamp < halfTime)
                    firstHalfIntensity += beatEvent.intensity;
                else
                    secondHalfIntensity += beatEvent.intensity;

                if (beatEvent.timestamp >= dropWindowStart)
                {
                    if (beatEvent.timestamp < dropHalfTime)
                        dropFirstHalfIntensity += beatEvent.intensity;
                    else
                        dropSecondHalfIntensity += beatEvent.intensity;
                }

                if (beatEvent.intensity >= input.HighAverageIntensity)
                    highTypes.Add(beatEvent.beatType);

                if (Math.Abs(beatEvent.timestamp - timestamp) <= input.SimultaneousBeatWindow &&
                    beatEvent.intensity >= input.HighAverageIntensity)
                {
                    simultaneousTypes.Add(beatEvent.beatType);
                }
            }

            float beatsPerSecond = beatCount / lookAhead;
            float averageIntensity = beatCount > 0 ? intensitySum / beatCount : 0f;
            bool rising = secondHalfIntensity > firstHalfIntensity * 1.15f;
            bool dropWindowRising = dropSecondHalfIntensity > dropFirstHalfIntensity * input.HypeDropRiseMultiplier;
            bool quiet = beatCount <= input.QuietWindowBeatThreshold;
            bool high = averageIntensity >= input.HighAverageIntensity &&
                        highTypes.Count >= input.HighUniqueBeatTypes &&
                        (rising || dropWindowRising || beatsPerSecond >= input.MediumBeatsPerSecond);

            HypeState state = HypeState.Medium;
            if (quiet)
                state = HypeState.Low;
            else if (high)
                state = HypeState.High;

            bool isMoshZone = simultaneousTypes.Count >= input.MoshUniqueBeatTypes;
            return new HypeSample(state, isMoshZone);
        }

        private readonly struct HypeSample
        {
            public readonly HypeState State;
            public readonly bool IsMoshZone;

            public HypeSample(HypeState state, bool isMoshZone)
            {
                State = state;
                IsMoshZone = isMoshZone;
            }
        }

        private void FillWindow(int frameOffset)
        {
            Array.Clear(leftImag, 0, leftImag.Length);
            Array.Clear(rightImag, 0, rightImag.Length);

            for (int i = 0; i < input.FftSize; i++)
            {
                int frame = frameOffset + i;
                float left = 0f;
                float right = 0f;

                if (frame < totalFrames)
                {
                    int sampleIndex = frame * input.Channels;
                    left = input.PcmSamples[sampleIndex];
                    right = input.Channels > 1 ? input.PcmSamples[sampleIndex + 1] : left;
                }

                leftReal[i] = left * window[i];
                rightReal[i] = right * window[i];
            }
        }

        private float CalculateRmsEnergy(int frameOffset)
        {
            double energy = 0.0;

            for (int i = 0; i < input.FftSize; i++)
            {
                int frame = frameOffset + i;
                float left = 0f;
                float right = 0f;

                if (frame < totalFrames)
                {
                    int sampleIndex = frame * input.Channels;
                    left = input.PcmSamples[sampleIndex];
                    right = input.Channels > 1 ? input.PcmSamples[sampleIndex + 1] : left;
                }

                energy += left * left + right * right;
            }

            return (float)Math.Sqrt(energy / (input.FftSize * 2.0));
        }

        private void FillSpectrums()
        {
            float scale = 1f / input.FftSize;

            for (int i = 0; i < spectrumLeft.Length; i++)
            {
                spectrumLeft[i] = Mathf.Sqrt(leftReal[i] * leftReal[i] + leftImag[i] * leftImag[i]) * scale;
                spectrumRight[i] = Mathf.Sqrt(rightReal[i] * rightReal[i] + rightImag[i] * rightImag[i]) * scale;
            }
        }

        private void CalculateFrequencyBands()
        {
            for (int i = 0; i < totalFreqBands; i++)
            {
                BeatDetection.FrequencyRange range = input.FrequencyRanges[i];
                currentFreqBands[i] = CalculateFrequencyRangeEnergy(range.lowFreq, range.highFreq);
            }
        }

        private float CalculateFrequencyRangeEnergy(float lowFreq, float highFreq)
        {
            int lowIndex = FrequencyToIndex(lowFreq);
            int highIndex = FrequencyToIndex(highFreq);

            if (highIndex < lowIndex)
            {
                int temp = lowIndex;
                lowIndex = highIndex;
                highIndex = temp;
            }

            float sum = 0f;
            int count = 0;

            for (int i = lowIndex; i <= highIndex && i < spectrumLeft.Length; i++)
            {
                float currentMagnitude = Math.Max(spectrumLeft[i], spectrumRight[i]);
                if (!hasPreviousSpectrum) continue;
                float previousMagnitude = Math.Max(previousSpectrumLeft[i], previousSpectrumRight[i]);
                // Log compression: log(1 + λ * x)
                // This emphasizes onsets in quieter parts of the frequency band.
                float currentLog = Mathf.Log(1f + 100f * currentMagnitude);
                float previousLog = Mathf.Log(1f + 100f * previousMagnitude);
                //float delta = currentMagnitude - previousMagnitude;
                float delta = currentLog - previousLog;
                sum += Math.Max(0f, delta);
                count++;
            }

            return count > 0 ? sum / count : 0f;
        }

        private void StoreCurrentSpectrumAsPrevious()
        {
            Array.Copy(spectrumLeft, previousSpectrumLeft, spectrumLeft.Length);
            Array.Copy(spectrumRight, previousSpectrumRight, spectrumRight.Length);
            hasPreviousSpectrum = true;
        }

        private int FrequencyToIndex(float frequency)
        {
            int index = (int)Math.Round(frequency * input.FftSize / input.ClipFrequency);
            return Math.Max(0, Math.Min(spectrumLeft.Length - 1, index));
        }

        private void DetectAndRecord(float songTime, List<BeatEvent> beatEvents)
        {
            if (historyCount < input.MinHistoryBeforeDetection)
                return;

            if (input.BeatMode == BeatDetection.BeatMode.Energy || input.BeatMode == BeatDetection.BeatMode.Both)
            {
                if (TryDetectEnergyBeat(songTime, out float intensity))
                    RecordBeatEvent(beatEvents, BeatDetection.BeatType.Energy, songTime, intensity);
            }

            if (input.BeatMode == BeatDetection.BeatMode.Frequency || input.BeatMode == BeatDetection.BeatMode.Both)
            {
                for (int i = 0; i < totalFreqBands; i++)
                {
                    BeatDetection.BeatType beatType = input.FrequencyRanges[i].beatType;
                    if (TryDetectBandBeat(i, songTime, beatType, out float intensity, out float detectedTimestamp))
                        RecordBeatEvent(beatEvents, beatType, detectedTimestamp, intensity);
                }
            }
        }

        private bool TryDetectEnergyBeat(float songTime, out float intensity)
        {
            float average = CalculateAverage(energyHistory, historyCount);
            float variance = CalculateVariance(energyHistory, average, historyCount);
            float threshold = input.EnergySensitivity * (average + variance * 0.5f);

            bool isBeat = currentEnergy > threshold &&
                          currentEnergy > input.MinEnergyThreshold &&
                          songTime - lastBeatTimeByType[BeatDetection.BeatType.Energy] >= input.MinBeatSeparation;

            intensity = isBeat ? CalculateIntensity(currentEnergy, threshold) : 0f;

            if (isBeat)
                lastBeatTimeByType[BeatDetection.BeatType.Energy] = songTime;

            return isBeat;
        }

        private bool TryDetectBandBeat(int bandIndex, float songTime, BeatDetection.BeatType beatType, out float intensity, out float detectedTimestamp)
        {
            float currentValue = currentFreqBands[bandIndex];
            float average = CalculateBandAverage(bandIndex);
            float variance = CalculateBandVariance(bandIndex, average);
            float adaptiveSensitivity = ComputeAdaptiveFrequencySensitivity(average, variance);
            float currentThreshold = adaptiveSensitivity * (average + variance * 0.3f);
            float effectiveMinSeparation = Math.Max(0.06f, input.MinBeatSeparation);

            intensity = 0f;
            detectedTimestamp = songTime;

            if (!hasPreviousFluxByBand[bandIndex])
            {
                previousFluxByBand[bandIndex] = currentValue;
                previousThresholdByBand[bandIndex] = currentThreshold;
                previousTimeByBand[bandIndex] = songTime;
                hasPreviousFluxByBand[bandIndex] = true;
                return false;
            }

            if (!hasPreviousPreviousFluxByBand[bandIndex])
            {
                previousPreviousFluxByBand[bandIndex] = previousFluxByBand[bandIndex];
                hasPreviousPreviousFluxByBand[bandIndex] = true;

                previousFluxByBand[bandIndex] = currentValue;
                previousThresholdByBand[bandIndex] = currentThreshold;
                previousTimeByBand[bandIndex] = songTime;
                return false;
            }

            float candidateFlux = previousFluxByBand[bandIndex];
            float candidateThreshold = previousThresholdByBand[bandIndex];
            float candidateTimestamp = previousTimeByBand[bandIndex];
            float leftNeighbor = previousPreviousFluxByBand[bandIndex];
            float rightNeighbor = currentValue;

            bool isPeak = candidateFlux > leftNeighbor && candidateFlux >= rightNeighbor;
            bool isBeat = isPeak &&
                          candidateFlux > candidateThreshold &&
                          candidateFlux > input.MinFrequencyThreshold &&
                          candidateTimestamp - lastBeatTimeByType[beatType] >= effectiveMinSeparation;

            if (isBeat)
            {
                intensity = CalculateIntensity(candidateFlux, candidateThreshold);
                detectedTimestamp = candidateTimestamp;
                lastBeatTimeByType[beatType] = candidateTimestamp;
            }

            previousPreviousFluxByBand[bandIndex] = previousFluxByBand[bandIndex];
            previousFluxByBand[bandIndex] = currentValue;
            previousThresholdByBand[bandIndex] = currentThreshold;
            previousTimeByBand[bandIndex] = songTime;

            return isBeat;
        }

        private float ComputeAdaptiveFrequencySensitivity(float average, float variance)
        {
            // Use normalized variance to approximate section "busyness":
            // higher values => stricter threshold to avoid ghost beats.
            float safeAverage = Math.Max(Epsilon, average);
            float stdDev = (float)Math.Sqrt(Math.Max(0f, variance));
            float cv = stdDev / safeAverage; // coefficient of variation

            // Map CV to a bounded multiplier around base sensitivity.
            // Quiet/stable sections (low CV): slightly more permissive.
            // Busy/dynamic sections (high CV): stricter detection.
            float adaptiveScale = Mathf.Lerp(0.9f, 1.55f, Mathf.Clamp01(cv / 2.2f));
            return input.FrequencySensitivity * adaptiveScale;
        }

        private void StoreCurrentFrameInHistory()
        {
            energyHistory[historyIndex] = currentEnergy;

            for (int i = 0; i < totalFreqBands; i++)
                freqBandHistory[i, historyIndex] = currentFreqBands[i];

            historyCount = Math.Min(historyCount + 1, input.HistoryLength);
            historyIndex = (historyIndex + 1) % input.HistoryLength;
        }

        private float CalculateAverage(float[] history, int count)
        {
            if (count <= 0)
                return 0f;

            double sum = 0.0;
            for (int i = 0; i < count; i++)
                sum += history[i];

            return (float)(sum / count);
        }

        private float CalculateVariance(float[] history, float average, int count)
        {
            if (count <= 1)
                return 0f;

            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                double diff = history[i] - average;
                sum += diff * diff;
            }

            return (float)(sum / (count - 1));
        }

        private float CalculateBandAverage(int bandIndex)
        {
            if (historyCount <= 0)
                return 0f;

            double sum = 0.0;
            for (int i = 0; i < historyCount; i++)
                sum += freqBandHistory[bandIndex, i];

            return (float)(sum / historyCount);
        }

        private float CalculateBandVariance(int bandIndex, float average)
        {
            if (historyCount <= 1)
                return 0f;

            double sum = 0.0;
            for (int i = 0; i < historyCount; i++)
            {
                double diff = freqBandHistory[bandIndex, i] - average;
                sum += diff * diff;
            }

            return (float)(sum / (historyCount - 1));
        }

        private float CalculateIntensity(float currentValue, float threshold)
        {
            if (threshold <= Epsilon)
                return 1f;

            float overThreshold = Math.Max(0f, currentValue - threshold);
            float normalized = overThreshold / Math.Max(Epsilon, threshold * input.IntensityNormalizeRange);
            return Clamp01(0.25f + normalized * 0.75f);
        }

        private void RecordBeatEvent(List<BeatEvent> beatEvents, BeatDetection.BeatType beatType, float timestamp, float intensity)
        {
            beatEvents.Add(new BeatEvent
            {
                beatType = beatType,
                timestamp = input.RoundTimestamps ? Round(timestamp, input.TimestampDecimals) : timestamp,
                intensity = input.RoundIntensities ? Round(intensity, input.IntensityDecimals) : intensity
            });
        }

        private static float[] CreateBlackmanHarrisWindow(int size)
        {
            float[] result = new float[size];
            const float a0 = 0.35875f;
            const float a1 = 0.48829f;
            const float a2 = 0.14128f;
            const float a3 = 0.01168f;
            float denominator = Math.Max(1, size - 1);

            for (int i = 0; i < size; i++)
            {
                float phase = 2f * Mathf.PI * i / denominator;
                result[i] = a0
                            - a1 * Mathf.Cos(phase)
                            + a2 * Mathf.Cos(2f * phase)
                            - a3 * Mathf.Cos(3f * phase);
            }

            return result;
        }

        private static void FastFourierTransform(float[] real, float[] imag)
        {
            int n = real.Length;
            int j = 0;

            for (int i = 1; i < n; i++)
            {
                int bit = n >> 1;
                while ((j & bit) != 0)
                {
                    j ^= bit;
                    bit >>= 1;
                }

                j ^= bit;

                if (i < j)
                {
                    Swap(real, i, j);
                    Swap(imag, i, j);
                }
            }

            for (int length = 2; length <= n; length <<= 1)
            {
                float angle = -2f * Mathf.PI / length;
                float wLengthReal = Mathf.Cos(angle);
                float wLengthImag = Mathf.Sin(angle);

                for (int i = 0; i < n; i += length)
                {
                    float wReal = 1f;
                    float wImag = 0f;
                    int halfLength = length >> 1;

                    for (int k = 0; k < halfLength; k++)
                    {
                        int evenIndex = i + k;
                        int oddIndex = evenIndex + halfLength;

                        float oddReal = real[oddIndex] * wReal - imag[oddIndex] * wImag;
                        float oddImag = real[oddIndex] * wImag + imag[oddIndex] * wReal;

                        real[oddIndex] = real[evenIndex] - oddReal;
                        imag[oddIndex] = imag[evenIndex] - oddImag;
                        real[evenIndex] += oddReal;
                        imag[evenIndex] += oddImag;

                        float nextWReal = wReal * wLengthReal - wImag * wLengthImag;
                        wImag = wReal * wLengthImag + wImag * wLengthReal;
                        wReal = nextWReal;
                    }
                }
            }
        }

        private static void Swap(float[] values, int a, int b)
        {
            float temp = values[a];
            values[a] = values[b];
            values[b] = temp;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            return value > 1f ? 1f : value;
        }

        private static float Round(float value, int decimals)
        {
            return (float)Math.Round(value, decimals);
        }
    }
}
