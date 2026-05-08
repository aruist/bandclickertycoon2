using System;
using TMPro;
using UnityEngine;

public class UIRegionTitleDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;

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
        if (titleText == null || paIkKaHaLlItSiJa.Instance == null) return;
        titleText.SetText(paIkKaHaLlItSiJa.Instance.GetCurrentRegionDisplayName());
    }
}
