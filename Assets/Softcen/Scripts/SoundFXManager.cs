using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Mobile-friendly SoundFXManager:
/// - Safe singleton bootstrap (no NRE if called early)
/// - UI mini-pool (pitch isolation)
/// - Gameplay SFX pool with starvation policy + optional "steal farthest"
/// - Optional per-clip cooldown + max instances
/// - No per-play coroutines (reaps finished sources in Update)
/// - Mixer routing support
/// </summary>
public sealed class SoundFXManager : MonoBehaviour
{
    public enum PoolStarvationBehavior
    {
        Skip,
        StealOldest,
        StealFarthest
    }

    public enum DefaultSounds
    {
        UI_KEYBOARD_CLICK,
        UI_DOUBLE_SPEED,
        UI_PURCHASE_UPGRADE,
        UI_STARPOPUP,
        UI_PICKUPCOINS,
        UI_POPUP,
        UI_BUBBLE,
        RELOAD,
        SHOOT,
        SHOOT_FAIL,
        COLLECTOR_HIT,
        SLIME_HIT,
        OPEN_DOOR,
        BUILDING_READY,
        VICTORY,
    }

    [Serializable]
    public struct DefaultSFX
    {
        public DefaultSounds sound;
        public AudioClip clip;
    }

    [Header("Default Sound Library")]
    [SerializeField] private List<DefaultSFX> defaultSFXes;// = new();

    [Header("Mixer Routing (Optional)")]
    [SerializeField] private AudioMixerGroup uiGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Global Toggles")]
    [SerializeField] private bool uiEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    [Header("UI Pool")]
    [SerializeField, Min(1)] private int uiPoolSize = 3;
    [SerializeField, Range(0f, 1f)] private float uiDefaultVolume = 1f;

    [Header("Gameplay SFX Pool")]
    [SerializeField, Min(1)] private int sfxPoolSize = 12; // 8–16 recommended on mobile
    [SerializeField] private PoolStarvationBehavior starvationBehavior = PoolStarvationBehavior.StealOldest;

    [Header("Voice Limiting")]
    [Tooltip("Minimum time between identical clips (seconds). 0 disables.")]
    [SerializeField] private float defaultClipCooldown = 0.05f;
    [Tooltip("Max simultaneous instances of the same clip. 0 disables.")]
    [SerializeField] private int defaultMaxInstancesPerClip = 2;

    [Header("Default 3D Settings (Gameplay SFX)")]
    [SerializeField, Range(0f, 1f)] private float defaultSpatialBlend = 1f; // 1 = 3D
    [SerializeField] private float defaultMinDistance = 1f;
    [SerializeField] private float defaultMaxDistance = 25f;
    [SerializeField] private float defaultDopplerLevel = 0f;
    [SerializeField] private AudioRolloffMode defaultRolloffMode = AudioRolloffMode.Logarithmic;

    // --------------------
    // Singleton bootstrap
    // --------------------
    private static SoundFXManager _instance;

    public static SoundFXManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindFirstObjectByType<SoundFXManager>();
            if (_instance == null)
            {
                var go = new GameObject(nameof(SoundFXManager));
                _instance = go.AddComponent<SoundFXManager>();
            }
            _instance.InitializeIfNeeded();
            return _instance;
        }
    }

    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // private static void Bootstrap()
    // {
    //     // Ensure singleton exists before any other script calls static audio methods.
    //     _ = Instance;
    // }

    private bool _initialized;

    private Dictionary<DefaultSounds, AudioClip> _defaultSounds;
    private AudioSource[] _uiSources;
    private int _uiRoundRobin;

    private sealed class PoolItem
    {
        public AudioSource src;
        public bool inUse;
        public float startTime;
        public AudioClip clip;
        public float maxDistance;
    }

    private readonly List<PoolItem> _sfxPool = new(16);

    // voice limiting
    private readonly Dictionary<AudioClip, float> _lastPlayTime = new(64);
    private readonly Dictionary<AudioClip, int> _clipActiveCount = new(64);

    // cached listener position (for steal-farthest)
    private Transform _listenerTransform;
    private float _nextListenerRefreshTime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (_initialized) return;
        _initialized = true;

        DontDestroyOnLoad(gameObject);

        BuildDefaultLibrary();
        CreateUIPool();
        CreateSFXPool();
        RefreshListener(force: true);
    }

    private void BuildDefaultLibrary()
    {
        _defaultSounds = new Dictionary<DefaultSounds, AudioClip>(Mathf.Max(8, defaultSFXes.Count));
        for (int i = 0; i < defaultSFXes.Count; i++)
        {
            var e = defaultSFXes[i];
            if (e.clip == null) continue;
            _defaultSounds[e.sound] = e.clip; // last wins
        }
    }

    private void CreateUIPool()
    {
        // rebuild if needed
        if (_uiSources != null && _uiSources.Length == uiPoolSize) return;

        // cleanup old
        if (_uiSources != null)
        {
            for (int i = 0; i < _uiSources.Length; i++)
            {
                if (_uiSources[i] != null) Destroy(_uiSources[i].gameObject);
            }
        }

        var root = transform.Find("UI_Pool");
        if (root == null)
        {
            var go = new GameObject("UI_Pool");
            go.transform.SetParent(transform, false);
            root = go.transform;
        }

        _uiSources = new AudioSource[Mathf.Max(1, uiPoolSize)];
        _uiRoundRobin = 0;

        for (int i = 0; i < _uiSources.Length; i++)
        {
            var child = new GameObject($"UI_{i:00}");
            child.transform.SetParent(root, false);
            var src = child.AddComponent<AudioSource>();
            ConfigureUISource(src);
            _uiSources[i] = src;
        }
    }

    private void CreateSFXPool()
    {
        var root = transform.Find("SFX_Pool");
        if (root == null)
        {
            var go = new GameObject("SFX_Pool");
            go.transform.SetParent(transform, false);
            root = go.transform;
        }

        // cleanup old children if pool size changed in inspector
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }

        _sfxPool.Clear();
        _sfxPool.Capacity = Mathf.Max(_sfxPool.Capacity, sfxPoolSize);

        for (int i = 0; i < sfxPoolSize; i++)
        {
            var child = new GameObject($"SFX_{i:00}");
            child.transform.SetParent(root, false);

            var src = child.AddComponent<AudioSource>();
            ConfigureSFXSourceDefaults(src);

            _sfxPool.Add(new PoolItem
            {
                src = src,
                inUse = false,
                startTime = -1f,
                clip = null,
                maxDistance = defaultMaxDistance
            });
        }

        _lastPlayTime.Clear();
        _clipActiveCount.Clear();
    }

    private void ConfigureUISource(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.outputAudioMixerGroup = uiGroup ? uiGroup : null;
        src.priority = 128;
    }

    private void ConfigureSFXSourceDefaults(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = defaultSpatialBlend;
        src.minDistance = Mathf.Max(0.01f, defaultMinDistance);
        src.maxDistance = Mathf.Max(src.minDistance, defaultMaxDistance);
        src.dopplerLevel = defaultDopplerLevel;
        src.rolloffMode = defaultRolloffMode;
        src.outputAudioMixerGroup = sfxGroup ? sfxGroup : null;
        src.priority = 128;
    }

    private void OnValidate()
    {
        uiPoolSize = Mathf.Max(1, uiPoolSize);
        sfxPoolSize = Mathf.Max(1, sfxPoolSize);
        defaultMinDistance = Mathf.Max(0.01f, defaultMinDistance);
        defaultMaxDistance = Mathf.Max(defaultMinDistance, defaultMaxDistance);
        defaultClipCooldown = Mathf.Max(0f, defaultClipCooldown);
        defaultMaxInstancesPerClip = Mathf.Max(0, defaultMaxInstancesPerClip);
        uiDefaultVolume = Mathf.Clamp01(uiDefaultVolume);
        defaultSpatialBlend = Mathf.Clamp01(defaultSpatialBlend);
    }

    private void Update()
    {
        // Reap finished SFX and keep counts correct.
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var item = _sfxPool[i];
            if (!item.inUse) continue;

            if (!item.src.isPlaying)
            {
                FreeItem(item);
            }
        }

        RefreshListener(force: false);
    }

    // -------------
    // Public API
    // -------------

    public static void SetUIEnabled(bool enabled)
    {
        var inst = Instance;
        inst.uiEnabled = enabled;
    }

    public static void SetSFXEnabled(bool enabled)
    {
        var inst = Instance;
        inst.sfxEnabled = enabled;
    }

    public static void PlayUIOneShot(DefaultSounds sound, float volume = 1f, float pitch = 1f)
        => Instance.InternalPlayUIOneShot(sound, volume, pitch);

    public static void PlayUIOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
        => Instance.InternalPlayUIOneShot(clip, volume, pitch);

    /// <summary>
    /// Plays a gameplay SFX at position with optional voice limiting.
    /// </summary>
    public static void PlaySFXAt(
        AudioClip clip,
        Vector3 position,
        float volume = 1f,
        float pitch = 1f,
        float spatialBlend = 1f,
        float maxDistance = 25f,
        float cooldown = -1f,
        int maxInstancesPerClip = -1)
        => Instance.InternalPlaySFXAt(clip, position, volume, pitch, spatialBlend, maxDistance, cooldown, maxInstancesPerClip);

    public static void PlaySFXAt(
        AudioClip clip,
        Transform at,
        float volume = 1f,
        float pitch = 1f,
        float spatialBlend = 1f,
        float maxDistance = 25f,
        float cooldown = -1f,
        int maxInstancesPerClip = -1)
    {
        if (at == null) return;
        Instance.InternalPlaySFXAt(clip, at.position, volume, pitch, spatialBlend, maxDistance, cooldown, maxInstancesPerClip);
    }

    // -------------------------
    // Internal Implementations
    // -------------------------

    private void InternalPlayUIOneShot(DefaultSounds sound, float volume, float pitch)
    {
        if (!uiEnabled) return;
        if (_defaultSounds == null) BuildDefaultLibrary();

        if (!_defaultSounds.TryGetValue(sound, out var clip) || clip == null)
            return;

        // #if SOFTCEN_DEBUG
        // Debug.Log($"InternalPlayUIOneShot sound: {sound}, {clip.name}", clip);
        // #endif
        InternalPlayUIOneShot(clip, volume, pitch);
    }

    private void InternalPlayUIOneShot(AudioClip clip, float volume, float pitch)
    {
        if (!uiEnabled) return;
        if (clip == null) return;

        InitializeIfNeeded();

        var src = GetNextUISource();
        src.pitch = Mathf.Clamp(pitch, -3f, 3f);
        src.outputAudioMixerGroup = uiGroup ? uiGroup : null;

        float finalVol = Mathf.Clamp01(uiDefaultVolume * volume);
        // Debug.Log($"InternalPlayUIOneShot sound: {clip.name}", clip);
        src.PlayOneShot(clip, finalVol);
    }

    private AudioSource GetNextUISource()
    {
        // Prefer a non-playing source first
        for (int i = 0; i < _uiSources.Length; i++)
        {
            int idx = (_uiRoundRobin + i) % _uiSources.Length;
            if (!_uiSources[idx].isPlaying)
            {
                _uiRoundRobin = (idx + 1) % _uiSources.Length;
                return _uiSources[idx];
            }
        }

        // All busy: steal round-robin
        var src = _uiSources[_uiRoundRobin];
        _uiRoundRobin = (_uiRoundRobin + 1) % _uiSources.Length;
        src.Stop();
        return src;
    }

    private void InternalPlaySFXAt(
        AudioClip clip,
        Vector3 position,
        float volume,
        float pitch,
        float spatialBlend,
        float maxDistance,
        float cooldown,
        int maxInstancesPerClip)
    {
        if (!sfxEnabled) return;
        if (clip == null) return;

        InitializeIfNeeded();

        // Voice limiting defaults
        float useCooldown = (cooldown >= 0f) ? cooldown : defaultClipCooldown;
        int useMaxInst = (maxInstancesPerClip >= 0) ? maxInstancesPerClip : defaultMaxInstancesPerClip;

        if (!CanPlayClip(clip, useCooldown, useMaxInst))
            return;

        var item = GetFreeSFXItem();
        if (item == null)
        {
            if (starvationBehavior == PoolStarvationBehavior.Skip)
                return;

            item = starvationBehavior switch
            {
                PoolStarvationBehavior.StealFarthest => GetFarthestInUseItem(position),
                _ => GetOldestInUseItem()
            };

            if (item == null) return;
            ForceStop(item);
        }

        StartPlayback(item, clip, position, volume, pitch, spatialBlend, maxDistance);
    }

    private bool CanPlayClip(AudioClip clip, float cooldown, int maxInstances)
    {
        var now = Time.unscaledTime;

        if (cooldown > 0f && _lastPlayTime.TryGetValue(clip, out var lastT))
        {
            if (now - lastT < cooldown) return false;
        }

        if (maxInstances > 0)
        {
            if (_clipActiveCount.TryGetValue(clip, out var count) && count >= maxInstances)
                return false;
        }

        _lastPlayTime[clip] = now;
        return true;
    }

    private PoolItem GetFreeSFXItem()
    {
        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var item = _sfxPool[i];
            if (!item.inUse && !item.src.isPlaying)
                return item;
        }
        return null;
    }

    private PoolItem GetOldestInUseItem()
    {
        PoolItem oldest = null;
        float oldestTime = float.PositiveInfinity;

        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var item = _sfxPool[i];
            if (!item.inUse) continue;

            if (item.startTime < oldestTime)
            {
                oldestTime = item.startTime;
                oldest = item;
            }
        }
        return oldest;
    }

    private PoolItem GetFarthestInUseItem(Vector3 referencePos)
    {
        RefreshListener(force: false);

        // If we have a listener, stealing farthest from listener generally sounds best.
        Vector3 pivot = _listenerTransform ? _listenerTransform.position : referencePos;

        PoolItem farthest = null;
        float farthestDist = float.NegativeInfinity;

        for (int i = 0; i < _sfxPool.Count; i++)
        {
            var item = _sfxPool[i];
            if (!item.inUse) continue;

            float d = (item.src.transform.position - pivot).sqrMagnitude;
            if (d > farthestDist)
            {
                farthestDist = d;
                farthest = item;
            }
        }

        return farthest;
    }

    private void StartPlayback(PoolItem item, AudioClip clip, Vector3 position, float volume, float pitch, float spatialBlend, float maxDistance)
    {
        item.inUse = true;
        item.startTime = Time.unscaledTime;
        item.clip = clip;
        item.maxDistance = Mathf.Max(defaultMinDistance, maxDistance);

        // Update per-clip active counts
        if (!_clipActiveCount.TryGetValue(clip, out var c)) c = 0;
        _clipActiveCount[clip] = c + 1;

        var src = item.src;
        src.transform.position = position;

        // per-play
        src.outputAudioMixerGroup = sfxGroup ? sfxGroup : null;
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.minDistance = Mathf.Max(0.01f, defaultMinDistance);
        src.maxDistance = item.maxDistance;

        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, -3f, 3f);

        src.clip = clip;
        src.loop = false;
        src.Play();
    }

    private void ForceStop(PoolItem item)
    {
        if (item.src.isPlaying)
            item.src.Stop();

        FreeItem(item);
    }

    private void FreeItem(PoolItem item)
    {
        // decrement counts
        if (item.clip != null)
        {
            if (_clipActiveCount.TryGetValue(item.clip, out var count))
            {
                count--;
                if (count <= 0) _clipActiveCount.Remove(item.clip);
                else _clipActiveCount[item.clip] = count;
            }
        }

        item.src.clip = null;
        item.clip = null;
        item.inUse = false;
        item.startTime = -1f;
    }

    private void RefreshListener(bool force)
    {
        // Refresh occasionally (listener might change on scene load)
        if (!force && Time.unscaledTime < _nextListenerRefreshTime) return;
        _nextListenerRefreshTime = Time.unscaledTime + 1.0f;

        if (_listenerTransform != null) return;

        var listener = FindFirstObjectByType<AudioListener>();
        _listenerTransform = listener ? listener.transform : null;
    }
}
/*
USAGE EXAMPLES:

// UI click sound
SoundFXManager.PlayUIOneShot(clickClip);

// 3D SFX at a position
SoundFXManager.PlaySFXAt(hitClip, transform.position);

// 3D SFX at any world coordinate
SoundFXManager.PlaySFXAt(explosionClip, new Vector3(10, 0, 5), volume: 1f, pitch: 1f, spatialBlend: 1f, maxDistance: 40f);

// Toggle
SoundFXManager.SetSFXEnabled(false);
*/
