using System;
using UnityEngine;

public class CubeScaler : MonoBehaviour
{
    public string eventID;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;
    public float maxTime = 0.3f;
    public float timer = 0.3f;
    public BeatDetection.BeatType type;

    void Start()
    {
        // Register for Koreography Events.  This sets up the callback.
        BeatPlay.OnBeatDetected += OnBeatDetected;
    }

    void Update()
    {
        if (timer < maxTime) {
            timer += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, maxTime-timer);

        }
    }

    void OnDestroy()
    {
        // Sometimes the Koreographer Instance gets cleaned up before hand.
        //  No need to worry in that case.
        BeatPlay.OnBeatDetected -= OnBeatDetected;
    }

    private void OnBeatDetected(BeatDetection.BeatType beatType, float intensity)
    {
        if (beatType == type)
        {
            timer = 0;
        }
    }

    // void AdjustScale(KoreographyEvent evt, int sampleTime, int sampleDelta, DeltaSlice deltaSlice)
	// 	{
	// 		if (evt.HasCurvePayload())
	// 		{
	// 			// Get the value of the curve at the current audio position.  This will be a
	// 			//  value between [0, 1] and will be used, below, to interpolate between
	// 			//  minScale and maxScale.
	// 			float curveValue = evt.GetValueOfCurveAtTime(sampleTime);

	// 			transform.localScale = Vector3.one * Mathf.Lerp(minScale, maxScale, curveValue);
	// 		}
	// 	}
	// }
}
