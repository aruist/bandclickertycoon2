using System;
using System.Collections;
using UnityEngine;

namespace Softcen.Clicker.Core
{
    [DisallowMultipleComponent]
    public sealed class ScenePoolPrewarm : MonoBehaviour, IScenePrewarmProvider
    {
        public enum ParticleFX
        {
            DISSAPEAR,
            LIGHTINGAPPEAR,
            ITEM1,
            AUDIENCE,
            COINCOLLECT,
            MONEYBLAST,
            START,
        }

        [Serializable]
        private struct FxEntry
        {
            public ParticleFX particleFX;
            public GameObject prefab;
            public int count;
        }

        [Header("Prewarm Entries ID")]
        [Header("Prewarm Entries")]
        [SerializeField] private FxEntry[] fxPools;

        [Header("Execution")]
        [SerializeField] private bool yieldBetweenEntries = true;
        [SerializeField] private int maxCreatesPerFrame = 32;

        void Start()
        {
            StartCoroutine(Prewarm());
        }

        public IEnumerator Prewarm()
        {
            #if SOFTCEN_DEBUG
            Debug.Log($"ScenePoolPrewarm - Prewarm");
            #endif
            yield return PrewarmFx();
        }

        private IEnumerator PrewarmFx()
        {
            if (fxPools == null)
            {
                yield break;
            }

            int createdThisFrame = 0;
            for (int i = 0; i < fxPools.Length; i++)
            {
                var entry = fxPools[i];
                int count = Mathf.Max(0, entry.count);
                for (int j = 0; j < count; j++)
                {
                    if (PooledFxPool.PrewarmOne(entry.prefab, (int)entry.particleFX))
                    {
                        createdThisFrame++;
                    }

                    if (ShouldYield(createdThisFrame))
                    {
                        createdThisFrame = 0;
                        yield return null;
                    }
                }

                if (yieldBetweenEntries)
                {
                    yield return null;
                    createdThisFrame = 0;
                }
            }
        }

        private bool ShouldYield(int createdThisFrame)
        {
            if (maxCreatesPerFrame <= 0 || createdThisFrame < maxCreatesPerFrame)
            {
                return false;
            }

            return true;
        }
    }
}
