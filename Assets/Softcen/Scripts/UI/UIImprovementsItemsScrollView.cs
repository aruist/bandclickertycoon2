using System;
using System.Collections.Generic;
using UnityEngine;

public class UIImprovementsItemsScrollView : MonoBehaviour
{
    [SerializeField] private List<ImprovementItemUI> items;
    [SerializeField] private Sprite[] icons;

    private RegionState regionState;
    void Awake()
    {
        for (int i=0; i < items.Count; i++)
        {
            items[i].gameObject.SetActive(false);
        }
    }

    void OnEnable()
    {
        paIkKaHaLlItSiJa.OnRegionChanged += OnRegionChanged;
    }

    void OnDisable()
    {
        paIkKaHaLlItSiJa.OnRegionChanged -= OnRegionChanged;
    }

    private void OnRegionChanged()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("UIImprovementsItemsScrollView OnRegionChanged");
        #endif
        regionState = paIkKaHaLlItSiJa.Instance.CurrentRegionState;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (regionState == null || regionState.Improvements == null || regionState.Improvements.Count <= 0) return;
        for (int i=0; i < items.Count; i++)
        {
            if (i < regionState.Improvements.Count)
            {
                if (regionState.Improvements[i] == null) continue;
                Debug.Log($"UIImprovementsItemsScrollView {regionState.Improvements[i].Definition.displayName}, level: {regionState.Improvements[i].Level}");
                Sprite icon = i < icons.Length ? icons[i] : null;
                items[i].Bind(regionState.Improvements[i], i, icon);
                items[i].gameObject.SetActive(true);
            }
            else
            {
                items[i].gameObject.SetActive(false);
            }
        }
    }
}
