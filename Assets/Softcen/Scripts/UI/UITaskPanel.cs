using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class UITaskPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textTaskTitle;
    [SerializeField] TextMeshProUGUI textTaskDescription;

    [SerializeField] private ImprovementDefinition currentDefinition;

    private void OnEnable()
    {
        // Subscribe globally to language change events
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
        UIImprovementTapButton.OnImprovementTapChanged += OnImprovementTapChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale locale)
    {
        RefreshUI();
    }

    private void OnImprovementTapChanged(int index)
    {
        currentDefinition = paIkKaHaLlItSiJa.Instance.GetCurrentRegionImprovement(index);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentDefinition == null) return;

        if (textTaskTitle != null) textTaskTitle.text = currentDefinition.localizedCardTitle.GetLocalizedString();
        if (textTaskDescription != null) textTaskDescription.text = currentDefinition.localizedCardTitle.GetLocalizedString();
    }
}
