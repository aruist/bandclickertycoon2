using UnityEngine;
using System.Collections;

public class StageDropExplosion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BeatDropDetector dropDetector;

    [Header("Particles")]
    [SerializeField] private ParticleSystem[] burstParticles;

    [Header("Objects To Flash")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private string colorPropertyName = "_BaseColor";
    [SerializeField] private float flashAlpha = 1f;
    [SerializeField] private float flashDuration = 0.28f;

    [Header("Optional Real Lights")]
    [SerializeField] private Light[] realLights;
    [SerializeField] private float maxLightIntensity = 6f;

    [Header("Optional Camera Shake")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeAmount = 0.06f;

    private MaterialPropertyBlock mpb;
    private int colorPropertyId;
    private Coroutine flashRoutine;
    private Vector3 originalCameraLocalPos;

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
        colorPropertyId = Shader.PropertyToID(colorPropertyName);

        if (cameraTransform != null)
            originalCameraLocalPos = cameraTransform.localPosition;
    }

    private void OnEnable()
    {
        if (dropDetector != null)
            dropDetector.DropDetected += TriggerExplosion;
    }

    private void OnDisable()
    {
        if (dropDetector != null)
            dropDetector.DropDetected -= TriggerExplosion;
    }

    public void TriggerExplosion(float strength)
    {
        strength = Mathf.Clamp01(strength);

        for (int i = 0; i < burstParticles.Length; i++)
        {
            if (burstParticles[i] != null)
                burstParticles[i].Play(true);
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(strength));
    }

    private IEnumerator FlashRoutine(float strength)
    {
        Color flashColor = Color.white;

        if (BeatStageColorSync.Instance != null)
            flashColor = BeatStageColorSync.Instance.GetColor(StageLightGroup.Drop);

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(timer / flashDuration);
            float alpha = flashAlpha * strength * t;

            SetFlashRenderers(flashColor, alpha);
            SetRealLights(maxLightIntensity * strength * t);
            ShakeCamera(t, strength);

            yield return null;
        }

        SetFlashRenderers(flashColor, 0f);
        SetRealLights(0f);

        if (cameraTransform != null)
            cameraTransform.localPosition = originalCameraLocalPos;
    }

    private void SetFlashRenderers(Color color, float alpha)
    {
        color.a = alpha;

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer r = flashRenderers[i];
            if (r == null)
                continue;

            r.GetPropertyBlock(mpb);
            mpb.SetColor(colorPropertyId, color);
            mpb.SetColor(Shader.PropertyToID("_Color"), color);
            r.SetPropertyBlock(mpb);
        }
    }

    private void SetRealLights(float intensity)
    {
        for (int i = 0; i < realLights.Length; i++)
        {
            if (realLights[i] != null)
                realLights[i].intensity = intensity;
        }
    }

    private void ShakeCamera(float fade, float strength)
    {
        if (cameraTransform == null)
            return;

        Vector3 offset = Random.insideUnitSphere * shakeAmount * strength * fade;
        offset.z = 0f;

        cameraTransform.localPosition = originalCameraLocalPos + offset;
    }
}
