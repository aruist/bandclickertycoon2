using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILanguageSettingsPanel : MonoBehaviour
{
    private const string CanvasMenuName = "CanvasMenu";
    private const string PanelRootName = "SettingsPanel_Language";

    private readonly List<Locale> _locales = new List<Locale>();

    private GameObject _panel;
    private TextMeshProUGUI _languageValueText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != "game")
            return;

        GameObject canvasMenu = GameObject.Find(CanvasMenuName);
        if (canvasMenu == null)
            return;

        if (canvasMenu.GetComponent<UILanguageSettingsPanel>() != null)
            return;

        canvasMenu.AddComponent<UILanguageSettingsPanel>();
    }

    private void Awake()
    {
        BuildPanel();
        RefreshLocales();
        RefreshLanguageLabel();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void OnSelectedLocaleChanged(Locale _)
    {
        RefreshLanguageLabel();
    }

    private void BuildPanel()
    {
        _panel = transform.Find(PanelRootName)?.gameObject;
        if (_panel != null)
            return;

        _panel = CreateUIObject(PanelRootName, transform, typeof(Image));
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-40f, -40f);
        panelRect.sizeDelta = new Vector2(420f, 220f);

        Image panelImage = _panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        CreateLabel("Title", _panel.transform, "SETTINGS", 44f, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f));
        CreateLabel("LanguageLabel", _panel.transform, "Language", 32f, new Vector2(0.05f, 0.48f), new Vector2(0.4f, 0.72f));

        _languageValueText = CreateLabel(
            "LanguageValue",
            _panel.transform,
            "-",
            32f,
            new Vector2(0.42f, 0.48f),
            new Vector2(0.95f, 0.72f),
            TextAlignmentOptions.Right);

        GameObject prevButton = CreateButton("PrevLanguageButton", _panel.transform, "<", new Vector2(0.05f, 0.1f), new Vector2(0.3f, 0.38f));
        GameObject nextButton = CreateButton("NextLanguageButton", _panel.transform, ">", new Vector2(0.7f, 0.1f), new Vector2(0.95f, 0.38f));

        prevButton.GetComponent<Button>().onClick.AddListener(SelectPreviousLocale);
        nextButton.GetComponent<Button>().onClick.AddListener(SelectNextLocale);

        GameObject toggleButton = CreateButton("SettingsToggleButton", transform, "Settings", new Vector2(1f, 1f), new Vector2(1f, 1f), true);
        RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
        toggleRect.pivot = new Vector2(1f, 1f);
        toggleRect.anchorMin = new Vector2(1f, 1f);
        toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.anchoredPosition = new Vector2(-40f, -280f);
        toggleRect.sizeDelta = new Vector2(220f, 70f);
        toggleButton.GetComponent<Button>().onClick.AddListener(TogglePanelVisibility);
    }

    private void TogglePanelVisibility()
    {
        if (_panel != null)
            _panel.SetActive(!_panel.activeSelf);
    }

    private void RefreshLocales()
    {
        _locales.Clear();
        if (LocalizationSettings.AvailableLocales != null)
            _locales.AddRange(LocalizationSettings.AvailableLocales.Locales);
    }

    private void SelectPreviousLocale()
    {
        SelectByOffset(-1);
    }

    private void SelectNextLocale()
    {
        SelectByOffset(1);
    }

    private void SelectByOffset(int offset)
    {
        RefreshLocales();
        if (_locales.Count == 0)
            return;

        Locale current = LocalizationSettings.SelectedLocale;
        int currentIndex = _locales.IndexOf(current);
        if (currentIndex < 0)
            currentIndex = 0;

        int nextIndex = (currentIndex + offset + _locales.Count) % _locales.Count;
        LocalizationSettings.SelectedLocale = _locales[nextIndex];
        RefreshLanguageLabel();
    }

    private void RefreshLanguageLabel()
    {
        if (_languageValueText == null)
            return;

        Locale locale = LocalizationSettings.SelectedLocale;
        if (locale == null)
        {
            _languageValueText.text = "-";
            return;
        }

        string code = locale.Identifier.Code;
        if (string.IsNullOrEmpty(code))
            code = locale.name;

        _languageValueText.text = $"{locale.LocaleName} ({code})";
    }

    private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject go = new GameObject(name, components);
        go.layer = 5;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect == null)
            rect = go.AddComponent<RectTransform>();

        return go;
    }

    private static TextMeshProUGUI CreateLabel(
        string name,
        Transform parent,
        string text,
        float fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        GameObject labelGo = CreateUIObject(name, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = labelGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static GameObject CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        bool absoluteSize = false)
    {
        GameObject buttonGo = CreateUIObject(name, parent, typeof(CanvasRenderer), typeof(Image), typeof(Button));

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        if (!absoluteSize)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(0.22f, 0.22f, 0.3f, 0.95f);

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        button.colors = colors;

        CreateLabel("Label", buttonGo.transform, label, 28f, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        return buttonGo;
    }
}
