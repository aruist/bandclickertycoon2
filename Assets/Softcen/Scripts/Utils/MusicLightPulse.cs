using UnityEngine;

public class MusicLightPulse : MonoBehaviour
{
    [SerializeField] private AudioSource music;
    [SerializeField] private Renderer beamRenderer;
    [SerializeField] private Light optionalRealLight;

    [SerializeField] private float sensitivity = 20f;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float minIntensity = 0.2f;
    [SerializeField] private float maxIntensity = 1.5f;

    private float[] samples = new float[64];
    private Material mat;
    private float currentPulse;

    private void Awake()
    {
        mat = beamRenderer.material;
    }

    private void Update()
    {
        music.GetOutputData(samples, 0);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        float rms = Mathf.Sqrt(sum / samples.Length);
        float targetPulse = Mathf.Clamp01(rms * sensitivity);

        currentPulse = Mathf.Lerp(currentPulse, targetPulse, Time.deltaTime * smoothSpeed);

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, currentPulse);

        Color color = mat.color;
        color.a = intensity;
        mat.color = color;

        if (optionalRealLight != null)
            optionalRealLight.intensity = intensity * 2f;

        transform.localScale = new Vector3(
            1f + currentPulse * 0.25f,
            1f,
            1f + currentPulse * 0.25f
        );
    }
}