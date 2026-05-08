using System;
using UnityEngine;

public class BoxScale : MonoBehaviour
{
    public Animation anim;

    void Start()
    {
        BeatPlay.OnBeatDetected += OnBeatDetected;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        if (beatType == BeatDetection.BeatType.Bass)
        {
            anim.Stop();
            anim.Play();

        }
    }

}
