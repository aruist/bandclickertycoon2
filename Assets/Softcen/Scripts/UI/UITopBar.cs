using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class UITopBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textPlaceName;
    [SerializeField] private TextMeshProUGUI textRegion;
    public LocalizedString localizedRegion;

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
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (paIkKaHaLlItSiJa.Instance == null) return;
        RegionState regionState = paIkKaHaLlItSiJa.Instance.CurrentRegionState;
        if (regionState == null) return;

        if (textPlaceName != null) textPlaceName.SetText(regionState.Definition.displayName);

    }
}
