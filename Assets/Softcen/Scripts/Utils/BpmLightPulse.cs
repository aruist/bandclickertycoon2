using UnityEngine;

public class BpmLightPulse : MonoBehaviour
{
    [SerializeField] private float bpm = 120f;
    [SerializeField] private Renderer beamRenderer;
    [SerializeField] private float pulseStrength = 1f;
    [SerializeField] private float fadeSpeed = 6f;

    private float beatInterval;
    private float timer;
    private float pulse;
    private Material mat;

    private void Awake()
    {
        beatInterval = 60f / bpm;
        mat = beamRenderer.material;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= beatInterval)
        {
            timer -= beatInterval;
            pulse = 1f;
        }

        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * fadeSpeed);

        Color c = mat.color;
        c.a = 0.2f + pulse * pulseStrength;
        mat.color = c;

        transform.localScale = Vector3.one * (1f + pulse * 0.15f);
    }
}