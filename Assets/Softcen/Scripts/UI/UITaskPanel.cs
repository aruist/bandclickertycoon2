using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class UITaskPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textTaskTitle;
    [SerializeField] TextMeshProUGUI textTaskDescription;
    [SerializeField] TextMeshProUGUI textPosterTitle;
    [SerializeField] Image imagePoster;
    [SerializeField] Button buttonDoIt;

    [SerializeField] private ImprovementDefinition currentDefinition;

    private int currentIndex;

    private void OnEnable()
    {
        // Subscribe globally to language change events
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
        UIImprovementTapButton.OnImprovementTapChanged += OnImprovementTapChanged;
        PlayerData.OnMoneyChanged += GameManager_OnMoneyChanged;

    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
        UIImprovementTapButton.OnImprovementTapChanged -= OnImprovementTapChanged;
        PlayerData.OnMoneyChanged -= GameManager_OnMoneyChanged;
    }

    private void GameManager_OnMoneyChanged()
    {
        paIkKaHaLlItSiJa ph = paIkKaHaLlItSiJa.Instance;
        if (ph == null) return;
    }
    private void OnLanguageChanged(Locale locale)
    {
        RefreshUI();
    }

    private void OnImprovementTapChanged(int index)
    {
        Debug.Log($"UITaskPanel OnImprovementTapChanged {index}");
        currentIndex = index;
        currentDefinition = paIkKaHaLlItSiJa.Instance.GetCurrentRegionImprovement(index);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (currentDefinition == null) return;
        if (currentDefinition.localizedCardTitle == null) return;
        if (currentDefinition.localizedCardDescription == null) return;

        if (textTaskTitle != null) textTaskTitle.SetText(currentDefinition.localizedCardTitle.GetLocalizedString());
        if (textTaskDescription != null) textTaskDescription.SetText(currentDefinition.localizedCardDescription.GetLocalizedString());
        if (textPosterTitle != null) textPosterTitle.SetText(currentDefinition.localizedPosterTitle.GetLocalizedString());
        if (imagePoster != null) imagePoster.sprite = currentDefinition.posterSprite;
    }

    public void DoItButtonPressed()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_PURCHASE_UPGRADE);
        paIkKaHaLlItSiJa.Instance.PurchaseImprovement(currentIndex);
    }
}
