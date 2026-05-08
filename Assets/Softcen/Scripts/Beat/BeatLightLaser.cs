using UnityEngine;
using static BeatDetection;

[RequireComponent(typeof(LineRenderer))]
public class BeatLightLaser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatPlay beatPlay;
    // public BeatPlay beatPlay;
    private LineRenderer lr;

    [Header("Beam")]
    public Transform target;
    public float baseWidth = 0.02f;
    public float maxWidth = 0.06f;
    public float baseAlpha = 0.05f;
    public float maxAlpha = 0.9f;

    [Header("Pulse")]
    public float attackSpeed = 40f;
    public float releaseSpeed = 18f;
    public float intensityMultiplier = 1.5f;
    public float randomness = 0.4f;

    [Tooltip("Which beat types should affect this beam.")]
    [SerializeField] private BeatDetection.BeatType reactToBeats =
        BeatDetection.BeatType.HiHat |
        BeatDetection.BeatType.Cymbal |
        BeatDetection.BeatType.High;

    // public BeatType[] reactTo =
    // {
    //     BeatType.HiHat,
    //     BeatType.Cymbal,
    //     BeatType.High
    // };

    private float pulse;
    private MaterialPropertyBlock mpb;
    private Renderer rend;

    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        lr.positionCount = 2;
    }

    private void OnEnable()
    {
        if (beatPlay != null)
            beatPlay.BeatDetected += OnBeatDetected;
            //beatPlay.BeatDetected += OnBeat;
    }


    private void OnDisable()
    {
        if (beatPlay != null)
            beatPlay.BeatDetected -= OnBeatDetected;
            //beatPlay.BeatDetected -= OnBeat;
    }

    private void Update()
    {
        // Position
        if (target != null)
        {
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, target.position);
        }

        // Fade pulse
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * releaseSpeed);

        float width = Mathf.Lerp(baseWidth, maxWidth, pulse);
        lr.startWidth = width;
        lr.endWidth = width;

        // Alpha
        float alpha = Mathf.Lerp(baseAlpha, maxAlpha, pulse);
        // If want totally disabled:
        //lr.enabled = alpha > 0.01f;

        rend.GetPropertyBlock(mpb);
        Color c = Color.white;
        c.a = alpha;
        mpb.SetColor(ColorID, c);
        rend.SetPropertyBlock(mpb);
    }

    private void OnBeatDetected(BeatType beatType, float intensity)
    {
        if ((reactToBeats & beatType) == 0)
            return;

        // Filter beat types
        // for (int i = 0; i < reactTo.Length; i++)
        // {
        //     if (beatType == reactTo[i])
        //     {
                float rnd = 1f + Random.Range(-randomness, randomness);
                float targetPulse = Mathf.Clamp01(intensity * intensityMultiplier * rnd);

                pulse = Mathf.Max(pulse, targetPulse);
        //         return;
        //     }
        // }
    }

    // private void OnBeat(BeatEvent e)
    // {
    //     // Filter beat types
    //     for (int i = 0; i < reactTo.Length; i++)
    //     {
    //         if (e.beatType == reactTo[i])
    //         {
    //             float rnd = 1f + Random.Range(-randomness, randomness);
    //             float targetPulse = Mathf.Clamp01(e.intensity * intensityMultiplier * rnd);

    //             pulse = Mathf.Max(pulse, targetPulse);
    //             return;
    //         }
    //     }
    // }
}