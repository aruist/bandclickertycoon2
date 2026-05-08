using System.Collections.Generic;
using UnityEngine;

public class AudienceManager : MonoBehaviour
{
    [SerializeField] private AudienceMember[] members;

    [Header("Pose Groups")]
    [SerializeField] private int[] idleFrames = { 0, 5, 10, 14 };
    [SerializeField] private int[] cheerFrames = { 1, 3, 7, 9, 15 };
    [SerializeField] private int[] clapFrames = { 2 };
    [SerializeField] private int[] waveFrames = { 4, 6, 13, 15 };

    private readonly List<AudienceMember> tempMembers = new();

    private void Awake()
    {
        if (members == null || members.Length == 0)
            members = GetComponentsInChildren<AudienceMember>();
    }

    private void OnEnable()
    {
        BeatPlay.OnBeatDetected += OnBeatDetected;
    }

    private void OnDisable()
    {
        BeatPlay.OnBeatDetected -= OnBeatDetected;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        switch (beatType)
        {
            case BeatDetection.BeatType.Kick:
                OnKickBeat(intensity);
                break;

            case BeatDetection.BeatType.Snare:
                OnSnareBeat(intensity);
                break;

            case BeatDetection.BeatType.HiHat:
                OnHighBeat(intensity);
                break;

            // case BeatDetection.BeatType.Drop:
            //     OnDropMoment(intensity);
            //     break;
        }
    }

    public void OnKickBeat(float strength)
    {
        PulseRandomMembers(0.75f, strength);
        ChangeRandomPoses(0.08f, cheerFrames);
    }

    public void OnSnareBeat(float strength)
    {
        PulseRandomMembers(0.35f, strength * 0.7f);
        ChangeRandomPoses(0.18f, clapFrames);
    }

    public void OnHighBeat(float strength)
    {
        ChangeRandomPoses(0.12f, waveFrames);
    }

    public void OnDropMoment()
    {
        for (int i = 0; i < members.Length; i++)
        {
            members[i].BeatPulse(1.5f);
            members[i].SetFrame(GetRandomFrame(cheerFrames));
        }
    }

    private void PulseRandomMembers(float chance, float strength)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (Random.value <= chance)
                members[i].BeatPulse(strength);
        }
    }

    private void ChangeRandomPoses(float chance, int[] frames)
    {
        for (int i = 0; i < members.Length; i++)
        {
            if (Random.value <= chance)
                members[i].SetFrame(GetRandomFrame(frames));
        }
    }

    private static int GetRandomFrame(int[] frames)
    {
        if (frames == null || frames.Length == 0)
            return 0;

        return frames[Random.Range(0, frames.Length)];
    }
}