using UnityEditor;
using UnityEngine;

public class AnalyzeInGameMusic : MonoBehaviour
{
    public PCMBeatDetection beatDetection;
    public AudioClip clip;
    #if UNITY_EDITOR
    void Start()
    {
        if (clip == null || beatDetection == null) return;

        string path = AssetDatabase.GetAssetPath(clip);
        Debug.Log($"{path}");
        beatDetection.AnalyzeMp3File(path, "Temp/test.json");
    }

    void OnEnable()
    {
        if (beatDetection == null) return;
        beatDetection.ProgressChanged += percent => Debug.Log(percent);
        beatDetection.AnalysisCompleted += (data, json) => Debug.Log($"AnalysisCompleted, beatEvent count: {data.beatEvents.Count}");
        beatDetection.AnalysisFailed += error => Debug.LogError(error);
    }

    void OnDisable()
    {
        if (beatDetection == null) return;
        beatDetection.ProgressChanged -= percent => Debug.Log(percent);
        beatDetection.AnalysisCompleted -= (data, json) => Debug.Log($"AnalysisCompleted, beatEvent count: {data.beatEvents.Count}");
        beatDetection.AnalysisFailed -= error => Debug.LogError(error);
    }
    #endif
}
