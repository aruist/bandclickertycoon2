using UnityEngine;
using System;
using static BeatDetection;

public class BeatDropDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatPlay beatPlay;

    [Header("Detection Window")]
    [Tooltip("How many seconds are checked for dense high-energy beats.")]
    [SerializeField] private float windowSeconds = 1.25f;

    [Tooltip("How many qualifying beats inside the window trigger a drop.")]
    [SerializeField] private int minimumBeatsInWindow = 5;

    [Tooltip("Minimum summed intensity inside the window.")]
    [SerializeField] private float minimumIntensitySum = 4.0f;

    [Tooltip("Prevents repeated drop explosions too close together.")]
    [SerializeField] private float cooldownSeconds = 6f;

    [Header("Beat Types That Can Trigger Drop")]
    [SerializeField] private BeatDetection.BeatType dropBeatTypes =
        BeatDetection.BeatType.Energy |
        BeatDetection.BeatType.Kick |
        BeatDetection.BeatType.Cymbal |
        BeatDetection.BeatType.BassDrum;

    // [SerializeField] private BeatType[] dropBeatTypes =
    // {
    //     BeatType.Energy,
    //     BeatType.Kick,
    //     BeatType.BassDrum,
    //     BeatType.Cymbal
    // };

    public event Action<float> DropDetected;

    private const int BufferSize = 64;

    private readonly float[] beatTimes = new float[BufferSize];
    private readonly float[] beatIntensities = new float[BufferSize];

    private int writeIndex;
    private int count;
    private float lastDropTime = -999f;

    private void OnEnable()
    {
        if (beatPlay != null)
            beatPlay.BeatTotalDetected += OnBeatDetected;
    }

    private void OnDisable()
    {
        if (beatPlay != null)
            beatPlay.BeatTotalDetected -= OnBeatDetected;
    }

    private void OnBeatDetected(BeatType beatType, float intensity, float timestamp)
    {
        if ((dropBeatTypes & beatType) == 0) return;

        float songTime = timestamp;

        beatTimes[writeIndex] = songTime;
        beatIntensities[writeIndex] = Mathf.Max(intensity, 0.01f);

        writeIndex = (writeIndex + 1) % BufferSize;
        count = Mathf.Min(count + 1, BufferSize);

        if (songTime - lastDropTime < cooldownSeconds)
            return;

        int beatsInWindow = 0;
        float intensitySum = 0f;
        float windowStart = songTime - windowSeconds;

        for (int i = 0; i < count; i++)
        {
            if (beatTimes[i] >= windowStart && beatTimes[i] <= songTime)
            {
                beatsInWindow++;
                intensitySum += beatIntensities[i];
            }
        }

        if (beatsInWindow >= minimumBeatsInWindow && intensitySum >= minimumIntensitySum)
        {
            lastDropTime = songTime;
            DropDetected?.Invoke(Mathf.Clamp01(intensitySum / (minimumIntensitySum * 1.5f)));
        }
    }

}
