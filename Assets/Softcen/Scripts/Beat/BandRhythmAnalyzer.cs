using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class BandRhythmAnalyzer : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Drum Detection Settings")]
    [SerializeField] private int drumFrequencyBand = 1; // Lower frequencies for drums
    [SerializeField] private float drumThreshold = 0.15f;
    [SerializeField] private float drumCooldown = 0.2f;
    [SerializeField] private float drumSensitivity = 1.5f;

    [Header("Guitar Detection Settings")]
    [SerializeField] private int guitarFrequencyBand = 4; // Mid frequencies for guitars
    [SerializeField] private float guitarThreshold = 0.1f;
    [SerializeField] private float guitarCooldown = 0.3f;
    [SerializeField] private float guitarSensitivity = 1.2f;

    [Header("Spectrum Analysis")]
    [SerializeField] private FFTWindow fftWindow = FFTWindow.Hamming;
    [SerializeField] private int spectrumSize = 512;
    [SerializeField] private float updateInterval = 0.05f;

    [Header("Events")]
    public UnityEvent OnDrummerMove;
    public UnityEvent OnGuitaristsMove;
    public UnityEvent<float> OnDrumIntensity; // Passes intensity value
    public UnityEvent<float> OnGuitarIntensity; // Passes intensity value

    private float[] spectrum;
    private float[] frequencyBands;
    private float[] bandBuffers;
    private float[] bufferDecrease;

    private float drumTimer;
    private float guitarTimer;
    private bool isAnalyzing = false;

    // Public properties for runtime adjustment
    public float DrumThreshold { get => drumThreshold; set => drumThreshold = value; }
    public float GuitarThreshold { get => guitarThreshold; set => guitarThreshold = value; }
    public float CurrentDrumIntensity { get; private set; }
    public float CurrentGuitarIntensity { get; private set; }

    private void Awake()
    {
        InitializeArrays();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void InitializeArrays()
    {
        spectrum = new float[spectrumSize];
        frequencyBands = new float[8]; // 8 frequency bands
        bandBuffers = new float[8];
        bufferDecrease = new float[8];
    }

    private void Start()
    {
        StartCoroutine(SpectrumAnalysisCoroutine());
    }

    private IEnumerator SpectrumAnalysisCoroutine()
    {
        isAnalyzing = true;

        while (isAnalyzing)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                AnalyzeSpectrum();
                CheckForDrummerMovement();
                CheckForGuitaristMovement();
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void AnalyzeSpectrum()
    {
        // Get spectrum data
        audioSource.GetSpectrumData(spectrum, 0, fftWindow);

        // Create frequency bands
        CreateFrequencyBands();

        // Apply buffer smoothing
        BandBuffer();
    }

    private void CreateFrequencyBands()
    {
        /*
        Frequency Bands:
        0: 0-86 Hz (Sub-bass)
        1: 87-258 Hz (Bass - drums)
        2: 259-602 Hz (Low mids)
        3: 603-1290 Hz (Mids)
        4: 1291-2666 Hz (Upper mids - guitars)
        5: 2667-5418 Hz (Presence)
        6: 5419-10922 Hz (Brilliance)
        7: 10923-22050 Hz (High end)
        */

        int count = 0;

        for (int i = 0; i < 8; i++)
        {
            int sampleCount = (int)Mathf.Pow(2, i) * 2;
            float average = 0;

            if (i == 7)
            {
                sampleCount += 2;
            }

            for (int j = 0; j < sampleCount; j++)
            {
                average += spectrum[count] * (count + 1);
                count++;
            }

            average /= count;
            frequencyBands[i] = average * 10;
        }
    }

    private void BandBuffer()
    {
        for (int i = 0; i < 8; i++)
        {
            if (frequencyBands[i] > bandBuffers[i])
            {
                bandBuffers[i] = frequencyBands[i];
                bufferDecrease[i] = 0.005f;
            }
            else
            {
                bandBuffers[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }
        }
    }

    private void CheckForDrummerMovement()
    {
        drumTimer -= updateInterval;

        if (drumTimer > 0) return;

        float drumIntensity = bandBuffers[drumFrequencyBand] * drumSensitivity;
        CurrentDrumIntensity = drumIntensity;

        OnDrumIntensity?.Invoke(drumIntensity);

        if (drumIntensity > drumThreshold)
        {
            OnDrummerMove?.Invoke();
            drumTimer = drumCooldown;

            Debug.Log($"Drummer MOVE! Intensity: {drumIntensity:F3}");
        }
    }

    private void CheckForGuitaristMovement()
    {
        guitarTimer -= updateInterval;

        if (guitarTimer > 0) return;

        float guitarIntensity = bandBuffers[guitarFrequencyBand] * guitarSensitivity;
        CurrentGuitarIntensity = guitarIntensity;

        OnGuitarIntensity?.Invoke(guitarIntensity);

        if (guitarIntensity > guitarThreshold)
        {
            OnGuitaristsMove?.Invoke();
            guitarTimer = guitarCooldown;

            Debug.Log($"Guitarists MOVE! Intensity: {guitarIntensity:F3}");
        }
    }

    public void StartAnalysis()
    {
        if (!isAnalyzing)
        {
            StartCoroutine(SpectrumAnalysisCoroutine());
        }
    }

    public void StopAnalysis()
    {
        isAnalyzing = false;
        StopAllCoroutines();
    }

    // Method to manually trigger events for testing
    public void TestDrummerMove()
    {
        OnDrummerMove?.Invoke();
    }

    public void TestGuitaristsMove()
    {
        OnGuitaristsMove?.Invoke();
    }

    // Runtime adjustment methods
    public void SetDrumSensitivity(float sensitivity)
    {
        drumSensitivity = Mathf.Clamp(sensitivity, 0.5f, 3f);
    }

    public void SetGuitarSensitivity(float sensitivity)
    {
        guitarSensitivity = Mathf.Clamp(sensitivity, 0.5f, 3f);
    }

    public void SetDrumFrequencyBand(int band)
    {
        drumFrequencyBand = Mathf.Clamp(band, 0, 7);
    }

    public void SetGuitarFrequencyBand(int band)
    {
        guitarFrequencyBand = Mathf.Clamp(band, 0, 7);
    }

    // Visualization helper methods
    public float GetFrequencyBand(int band)
    {
        if (band >= 0 && band < 8)
            return bandBuffers[band];
        return 0f;
    }

    public float[] GetAllFrequencyBands()
    {
        return bandBuffers;
    }

    private void OnDestroy()
    {
        StopAnalysis();
    }

    private void OnEnable()
    {
        if (isAnalyzing)
        {
            StartAnalysis();
        }
    }

    private void OnDisable()
    {
        StopAnalysis();
    }
}