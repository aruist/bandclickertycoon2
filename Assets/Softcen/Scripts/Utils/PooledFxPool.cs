using System.Collections.Generic;
using Softcen.Clicker.Core;
using UnityEngine;

/// <summary>
/// Lightweight GameObject pool for timed FX instances keyed by prefab.
/// </summary>
public static class PooledFxPool
{
    private sealed class PoolData
    {
        public GameObject Prefab;
        public Transform Parent;
        public readonly Queue<PooledFxHandle> Inactive = new Queue<PooledFxHandle>();
    }

    private static readonly Dictionary<int, PoolData> Pools = new Dictionary<int, PoolData>();
    private static Transform _root;

    // public static void Prewarm(GameObject prefab, int count)
    // {
    //     if (prefab == null || count <= 0)
    //     {
    //         return;
    //     }

    //     var pool = GetOrCreatePool(prefab);
    //     int missing = count - pool.Inactive.Count;
    //     for (int i = 0; i < missing; i++)
    //     {
    //         PrewarmOne(prefab);
    //     }
    // }

    public static bool PrewarmOne(GameObject prefab, int id)
    {
        if (prefab == null)
        {
            return false;
        }

        var pool = GetOrCreatePool(prefab, id);
        var instance = CreateInstance(pool);
        pool.Inactive.Enqueue(instance);
        return true;
    }

    public static PooledFxHandle Spawn(ScenePoolPrewarm.ParticleFX particleFX, Vector3 position)
    {
        var pool = GetPool((int)particleFX);
        if (pool == null) return null;

        PooledFxHandle handle = null;
        while (pool.Inactive.Count > 0 && handle == null)
        {
            handle = pool.Inactive.Dequeue();
        }

        if (handle == null)
        {
            handle = CreateInstance(pool);
        }

        handle.PrepareForSpawn();
        var transform = handle.CachedTransform;
        transform.position = position;
        // transform.SetPositionAndRotation(position, rotation);
        // if (parent != null) transform.SetParent(parent, true);
        // // Debug.Log("Particle SetActive");
        handle.CachedGameObject.SetActive(true);
        return handle;
    }

    public static PooledFxHandle Spawn(ScenePoolPrewarm.ParticleFX particleFX, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var pool = GetPool((int)particleFX);
        if (pool == null) return null;

        PooledFxHandle handle = null;
        while (pool.Inactive.Count > 0 && handle == null)
        {
            handle = pool.Inactive.Dequeue();
        }

        if (handle == null)
        {
            handle = CreateInstance(pool);
        }

        handle.PrepareForSpawn();
        var transform = handle.CachedTransform;
        transform.SetPositionAndRotation(position, rotation);
        if (parent != null) transform.SetParent(parent, true);
        // Debug.Log("Particle SetActive");
        handle.CachedGameObject.SetActive(true);
        return handle;
    }

    public static void ScheduleRelease(PooledFxHandle handle, float lifetimeSeconds)
    {
        if (handle == null)
        {
            return;
        }

        if (lifetimeSeconds <= 0f)
        {
            Release(handle);
            return;
        }

        handle.ScheduleRelease(lifetimeSeconds);
    }

    public static void Release(PooledFxHandle handle)
    {
        if (handle == null)
        {
            return;
        }

        int key = handle.PrefabKey;
        if (key == 0 || !Pools.TryGetValue(key, out var pool))
        {
            Object.Destroy(handle.CachedGameObject);
            return;
        }

        handle.PrepareForRelease();
        handle.CachedTransform.SetParent(pool.Parent, false);
        handle.CachedGameObject.SetActive(false);
        pool.Inactive.Enqueue(handle);
    }

    public static void ClearAll()
    {
        foreach (var kv in Pools)
        {
            var pool = kv.Value;
            if (pool == null)
            {
                continue;
            }

            while (pool.Inactive.Count > 0)
            {
                var handle = pool.Inactive.Dequeue();
                if (handle != null)
                {
                    Object.Destroy(handle.CachedGameObject);
                }
            }

            if (pool.Parent != null)
            {
                Object.Destroy(pool.Parent.gameObject);
            }
        }

        Pools.Clear();
        if (_root != null)
        {
            Object.Destroy(_root.gameObject);
            _root = null;
        }
    }

    private static PoolData GetPool(int id)
    {
        int key = id;
        if (Pools.TryGetValue(key, out var pool))
        {
            return pool;
        }
        return null;
    }

    private static PoolData GetOrCreatePool(GameObject prefab, int id)
    {
        int key = id; //prefab.GetInstanceID();
        if (Pools.TryGetValue(key, out var pool))
        {
            return pool;
        }

        var parentGO = new GameObject($"FxPool_{prefab.name}");
        parentGO.transform.SetParent(GetRoot(), false);
        pool = new PoolData
        {
            Prefab = prefab,
            Parent = parentGO.transform
        };
        Pools[id] = pool;
        return pool;
    }

    private static PooledFxHandle CreateInstance(PoolData pool)
    {
        var instance = Object.Instantiate(pool.Prefab, pool.Parent);
        var handle = instance.GetComponent<PooledFxHandle>();
        if (handle == null)
        {
            handle = instance.AddComponent<PooledFxHandle>();
        }

        handle.Initialize(pool.Prefab.GetInstanceID());
        instance.SetActive(false);
        return handle;
    }

    private static Transform GetRoot()
    {
        if (_root != null)
        {
            return _root;
        }

        var rootGO = new GameObject("[Pool] FX");
        Object.DontDestroyOnLoad(rootGO);
        _root = rootGO.transform;
        return _root;
    }
}
