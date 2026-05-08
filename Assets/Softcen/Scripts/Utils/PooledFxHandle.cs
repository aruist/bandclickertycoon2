using UnityEngine;

/// <summary>
/// Runtime metadata and timed-release helper for pooled FX instances.
/// </summary>
public sealed class PooledFxHandle : MonoBehaviour
{
    private Transform _cachedTransform;
    private GameObject _cachedGameObject;
    private ParticleSystem[] _particleSystems;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;
    private Vector3 _initialLocalScale;
    private float _releaseTimer;
    private bool _releaseScheduled;

    public int PrefabKey { get; private set; }
    public Transform CachedTransform => _cachedTransform;
    public GameObject CachedGameObject => _cachedGameObject;

    private void Awake()
    {
        _cachedTransform = transform;
        _cachedGameObject = gameObject;
        _initialLocalPosition = _cachedTransform.localPosition;
        _initialLocalRotation = _cachedTransform.localRotation;
        _initialLocalScale = _cachedTransform.localScale;
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        enabled = false;
    }

    public void Initialize(int prefabKey)
    {
        PrefabKey = prefabKey;
    }

    public void PrepareForSpawn()
    {
        _releaseScheduled = false;
        _releaseTimer = 0f;
        enabled = false;
        _cachedTransform.localPosition = _initialLocalPosition;
        _cachedTransform.localRotation = _initialLocalRotation;
        _cachedTransform.localScale = _initialLocalScale;
        if (_particleSystems == null)
        {
            return;
        }

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var particleSystem = _particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    public void ScheduleRelease(float lifetimeSeconds)
    {
        if (lifetimeSeconds <= 0f)
        {
            PooledFxPool.Release(this);
            return;
        }

        _releaseScheduled = true;
        _releaseTimer = lifetimeSeconds;
        enabled = true;
    }

    public void PrepareForRelease()
    {
        _releaseScheduled = false;
        _releaseTimer = 0f;
        enabled = false;
        if (_particleSystems == null)
        {
            return;
        }

        for (int i = 0; i < _particleSystems.Length; i++)
        {
            var particleSystem = _particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (!_releaseScheduled)
        {
            return;
        }

        _releaseTimer -= Time.deltaTime;
        if (_releaseTimer > 0f)
        {
            return;
        }

        _releaseScheduled = false;
        _releaseTimer = 0f;
        enabled = false;
        PooledFxPool.Release(this);
    }
}
