using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserSongImportUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SongLibraryManager songLibraryManager;

    [Header("UI")]
    [SerializeField] private Button importSongButton;
    [SerializeField] private Button cancelAnalysisButton;
    [SerializeField] private Button selectSongButton;
    [SerializeField] private Button playSelectedButton;
    [SerializeField] private TMP_Dropdown songsDropdown;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;

    private int selectedSongIndex;
    private bool busy;

    private void Awake()
    {

        if (songLibraryManager == null)
            songLibraryManager = SongLibraryManager.Instance != null
                ? SongLibraryManager.Instance
                : FindFirstObjectByType<SongLibraryManager>();
    }

    private void Start()
    {
        if (songLibraryManager == null)
        {
            UpdateStatus("SongLibraryManager missing.");
            return;
        }

        songLibraryManager.OnLibraryChanged += RefreshSongsDropdown;
        songLibraryManager.OnAnalysisProgress += OnAnalysisProgress;
        songLibraryManager.OnAnalysisFailed += OnAnalysisFailed;

        RefreshSongsDropdown();
        SetIdleUI();
    }

    private void OnDestroy()
    {
        if (songLibraryManager == null)
            return;

        songLibraryManager.OnLibraryChanged -= RefreshSongsDropdown;
        songLibraryManager.OnAnalysisProgress -= OnAnalysisProgress;
        songLibraryManager.OnAnalysisFailed -= OnAnalysisFailed;
    }

    private void OnEnable()
    {
        if (importSongButton != null)
            importSongButton.onClick.AddListener(OnImportSongClicked);

        if (cancelAnalysisButton != null)
            cancelAnalysisButton.onClick.AddListener(OnCancelAnalysisClicked);

        if (playSelectedButton != null)
            playSelectedButton.onClick.AddListener(OnPlaySelectedClicked);

        if (selectSongButton != null)
            selectSongButton.onClick.AddListener(OnSelectSongClicked);

        if (songsDropdown != null)
            songsDropdown.onValueChanged.AddListener(OnSongDropdownValueChanged);
    }

    private void OnDisable()
    {
        if (importSongButton != null)
            importSongButton.onClick.RemoveListener(OnImportSongClicked);

        if (cancelAnalysisButton != null)
            cancelAnalysisButton.onClick.RemoveListener(OnCancelAnalysisClicked);

        if (playSelectedButton != null)
            playSelectedButton.onClick.RemoveListener(OnPlaySelectedClicked);

        if (selectSongButton != null)
            selectSongButton.onClick.RemoveListener(OnSelectSongClicked);

        if (songsDropdown != null)
            songsDropdown.onValueChanged.RemoveListener(OnSongDropdownValueChanged);
    }

    private void OnImportSongClicked()
    {
        if (busy || songLibraryManager == null)
            return;

        SetBusyUI();
        UpdateStatus("Opening file picker...");
        NativeFilePicker.PickFile(OnFilePicked, GetPickerFileTypes());
    }

    private void OnFilePicked(string pickedPath)
    {
        if (string.IsNullOrWhiteSpace(pickedPath))
        {
            UpdateStatus("Import cancelled.");
            SetIdleUI();
            return;
        }

        songLibraryManager.ImportSong(pickedPath, (success, message, entry) =>
        {
            UpdateStatus(message);
            if (success && entry != null)
                LoadAndPlaySong(entry.songId);
            else
                SetIdleUI();
        });
    }

    private static string[] GetPickerFileTypes()
    {
#if UNITY_IOS && !UNITY_EDITOR
        string mp3Uti = NativeFilePicker.ConvertExtensionToFileType("mp3");
        if (string.IsNullOrEmpty(mp3Uti))
            mp3Uti = "public.mp3";
        return new[] { "public.audio", mp3Uti };
#elif UNITY_ANDROID && !UNITY_EDITOR
        return new[] { "audio/*" };
#else
        return new[] { "audio/*", ".mp3" };
#endif
    }

    private void OnCancelAnalysisClicked()
    {
        if (songLibraryManager == null)
            return;

        songLibraryManager.CancelCurrentAnalysis();
        UpdateStatus("Cancelling analysis...");
    }

    private void OnSelectSongClicked()
    {
        if (songLibraryManager == null || songLibraryManager.Songs.Count == 0)
        {
            UpdateStatus("No songs.");
            return;
        }

        if (songsDropdown != null)
        {
            songsDropdown.gameObject.SetActive(true);
            songsDropdown.Show();
            UpdateStatus("Select a song from the list.");
        }
    }

    private void OnSongDropdownValueChanged(int index)
    {
        selectedSongIndex = Mathf.Clamp(index, 0, Mathf.Max(0, songLibraryManager.Songs.Count - 1));
        if (songLibraryManager.Songs.Count > selectedSongIndex)
            UpdateStatus($"Selected: {songLibraryManager.Songs[selectedSongIndex].displayName}");
    }

    private void OnPlaySelectedClicked()
    {
        if (songLibraryManager == null || songLibraryManager.Songs.Count == 0)
        {
            UpdateStatus("No songs.");
            return;
        }

        int index = songsDropdown != null && songsDropdown.options.Count > 0
            ? Mathf.Clamp(songsDropdown.value, 0, songLibraryManager.Songs.Count - 1)
            : Mathf.Clamp(selectedSongIndex, 0, songLibraryManager.Songs.Count - 1);

        LoadAndPlaySong(songLibraryManager.Songs[index].songId);
    }

    private void LoadAndPlaySong(string songId)
    {
        if (songLibraryManager == null)
            return;

        SetBusyUI();
        UpdateStatus("Loading song...");
        songLibraryManager.LoadAndPlaySong(songId, (success, message) =>
        {
            UpdateStatus(message);
            SetIdleUI();
        });
    }

    private void RefreshSongsDropdown()
    {
        if (songLibraryManager == null || songsDropdown == null)
            return;

        songsDropdown.ClearOptions();
        List<string> labels = new List<string>();
        IReadOnlyList<SongLibraryEntry> songs = songLibraryManager.Songs;

        for (int i = 0; i < songs.Count; i++)
            labels.Add(songs[i].displayName);

        if (labels.Count == 0)
            labels.Add("No songs");

        songsDropdown.AddOptions(labels);
        selectedSongIndex = songs.Count > 0 ? Mathf.Clamp(selectedSongIndex, 0, songs.Count - 1) : 0;
        songsDropdown.value = selectedSongIndex;
        songsDropdown.RefreshShownValue();
        songsDropdown.gameObject.SetActive(songs.Count > 0);

        if (selectSongButton != null)
            selectSongButton.interactable = songs.Count > 0;
    }

    private void OnAnalysisProgress(string songId, float progress)
    {
        if (progressSlider != null)
            progressSlider.value = progress;

        UpdateProgressText(progress * 100f);
    }

    private void OnAnalysisFailed(string message)
    {
        UpdateStatus($"Analysis failed: {message}");
        SetIdleUI();
    }

    private void SetBusyUI()
    {
        busy = true;
        if (importSongButton != null)
            importSongButton.interactable = false;
        if (cancelAnalysisButton != null)
            cancelAnalysisButton.gameObject.SetActive(true);
    }

    private void SetIdleUI()
    {
        busy = false;
        if (importSongButton != null)
            importSongButton.interactable = true;
        if (cancelAnalysisButton != null)
            cancelAnalysisButton.gameObject.SetActive(false);
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    private void UpdateProgressText(float percent)
    {
        if (progressText != null)
            progressText.text = $"{percent:0}%";
    }
}
