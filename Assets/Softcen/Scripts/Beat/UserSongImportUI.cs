using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class UserSongManifestEntry
{
    public string id;
    public string displayName;
    public string originalFileName;
    public string audioPath;
    public string beatDataPath;
    public float duration;
    public int clipFrequency;
    public int analysisVersion = 1;
}

[Serializable]
public class UserSongManifest
{
    public List<UserSongManifestEntry> songs = new List<UserSongManifestEntry>();
}

public class UserSongImportUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private BeatPlay beatPlay;
    [SerializeField] private PCMBeatDetection pcmBeatDetection;
    [SerializeField] private AudioSource playbackAudioSource;

    [Header("UI")]
    [SerializeField] private Button importSongButton;
    [SerializeField] private Button cancelAnalysisButton;
    [SerializeField] private Button playSelectedButton;
    [SerializeField] private TMP_Dropdown songsDropdown;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Limits")]
    [SerializeField, Min(30f)] private float maxSongDurationSeconds = 600f;

    private UserSongManifest manifest = new UserSongManifest();
    private Coroutine importCoroutine;

    private string UserSongsRoot => Path.Combine(Application.persistentDataPath, "UserSongs");
    private string ManifestPath => Path.Combine(UserSongsRoot, "songs_manifest.json");

    private void Awake()
    {
        if (beatPlay == null)
            beatPlay = FindFirstObjectByType<BeatPlay>();

        if (pcmBeatDetection == null)
            pcmBeatDetection = FindFirstObjectByType<PCMBeatDetection>();

        if (playbackAudioSource == null && beatPlay != null)
            playbackAudioSource = beatPlay.GetComponent<AudioSource>();
    }

    private void Start()
    {
        Directory.CreateDirectory(UserSongsRoot);
        LoadManifest();
        RefreshSongsDropdown();
        SetIdleUI();
    }

    private void OnEnable()
    {
        if (importSongButton != null)
            importSongButton.onClick.AddListener(OnImportSongClicked);

        if (cancelAnalysisButton != null)
            cancelAnalysisButton.onClick.AddListener(OnCancelAnalysisClicked);

        if (playSelectedButton != null)
            playSelectedButton.onClick.AddListener(OnPlaySelectedClicked);
    }

    private void OnDisable()
    {
        if (importSongButton != null)
            importSongButton.onClick.RemoveListener(OnImportSongClicked);

        if (cancelAnalysisButton != null)
            cancelAnalysisButton.onClick.RemoveListener(OnCancelAnalysisClicked);

        if (playSelectedButton != null)
            playSelectedButton.onClick.RemoveListener(OnPlaySelectedClicked);
    }

    private void OnImportSongClicked()
    {
        if (importCoroutine != null || pcmBeatDetection == null)
            return;

        UpdateStatus("Opening file picker...");
        NativeFilePicker.PickFile(OnFilePicked, "audio/*", ".mp3");
    }

    private void OnCancelAnalysisClicked()
    {
        if (pcmBeatDetection != null && pcmBeatDetection.IsRunning)
        {
            pcmBeatDetection.CancelAnalysis();
            UpdateStatus("Cancelling analysis...");
        }
    }

    private void OnPlaySelectedClicked()
    {
        if (manifest == null || manifest.songs == null || manifest.songs.Count == 0)
        {
            UpdateStatus("No imported songs.");
            return;
        }

        int index = manifest.songs.Count - 1;
        if (songsDropdown != null && songsDropdown.options.Count > 0)
            index = Mathf.Clamp(songsDropdown.value, 0, manifest.songs.Count - 1);

        if (index < 0 || index >= manifest.songs.Count)
            return;

        StartCoroutine(LoadAndPlaySong(manifest.songs[index]));
    }

    private void OnFilePicked(string pickedPath)
    {
        if (string.IsNullOrWhiteSpace(pickedPath))
        {
            UpdateStatus("Import cancelled.");
            return;
        }

        if (!File.Exists(pickedPath))
        {
            UpdateStatus("Picked file is not accessible.");
            return;
        }

        importCoroutine = StartCoroutine(ImportSongRoutine(pickedPath));
    }

    private IEnumerator ImportSongRoutine(string sourcePath)
    {
        SetBusyUI();
        progressSlider.value = 0f;
        UpdateProgressText(0f);

        string originalName = Path.GetFileName(sourcePath);
        byte[] bytes;
        try
        {
            UpdateStatus("Copying song...");
            bytes = File.ReadAllBytes(sourcePath);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Copy failed: {ex.Message}");
            SetIdleUI();
            importCoroutine = null;
            yield break;
        }

        string songId = ComputeSha1(bytes);
        string songFolder = Path.Combine(UserSongsRoot, songId);
        string targetMp3Path = Path.Combine(songFolder, "original.mp3");
        string beatJsonPath = Path.Combine(songFolder, "beatdata.json");

        Directory.CreateDirectory(songFolder);
        File.WriteAllBytes(targetMp3Path, bytes);
        bytes = null;

        UpdateStatus("Analyzing beats...");
        pcmBeatDetection.AnalyzeMp3File(targetMp3Path, beatJsonPath);

        while (pcmBeatDetection.IsRunning)
        {
            float p = Mathf.Clamp01(pcmBeatDetection.ProgressPercentage / 100f);
            progressSlider.value = p;
            UpdateProgressText(p * 100f);
            yield return null;
        }

        if (!pcmBeatDetection.IsComplete || !string.IsNullOrEmpty(pcmBeatDetection.ErrorMessage))
        {
            UpdateStatus($"Analysis failed: {pcmBeatDetection.ErrorMessage}");
            SetIdleUI();
            importCoroutine = null;
            yield break;
        }

        BeatData beatData = pcmBeatDetection.LastBeatData;
        if (beatData == null || beatData.beatEvents == null || beatData.beatEvents.Count == 0)
        {
            UpdateStatus("Analysis produced no beat data.");
            SetIdleUI();
            importCoroutine = null;
            yield break;
        }

        if (beatData.audioLength > maxSongDurationSeconds)
        {
            UpdateStatus($"Song too long ({beatData.audioLength:0}s). Max is {maxSongDurationSeconds:0}s.");
            SetIdleUI();
            importCoroutine = null;
            yield break;
        }

        UserSongManifestEntry entry = FindEntryById(songId);
        if (entry == null)
        {
            entry = new UserSongManifestEntry();
            manifest.songs.Add(entry);
        }

        entry.id = songId;
        entry.displayName = Path.GetFileNameWithoutExtension(originalName);
        entry.originalFileName = originalName;
        entry.audioPath = targetMp3Path;
        entry.beatDataPath = beatJsonPath;
        entry.duration = beatData.audioLength;
        entry.clipFrequency = beatData.clipFrequency;
        entry.analysisVersion = 1;

        SaveManifest();
        RefreshSongsDropdown();
        progressSlider.value = 1f;
        UpdateProgressText(100f);
        UpdateStatus("Song imported.");

        yield return LoadAndPlaySong(entry);

        SetIdleUI();
        importCoroutine = null;
    }

    private IEnumerator LoadAndPlaySong(UserSongManifestEntry entry)
    {
        if (entry == null || beatPlay == null || playbackAudioSource == null)
        {
            UpdateStatus("Playback setup missing.");
            yield break;
        }

        if (!File.Exists(entry.audioPath) || !File.Exists(entry.beatDataPath))
        {
            UpdateStatus("Song files missing.");
            yield break;
        }
        Debug.Log($"{entry.beatDataPath}");
        UpdateStatus("Loading song...");
        string uri = new Uri(Path.GetFullPath(entry.audioPath)).AbsoluteUri;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                UpdateStatus($"Audio load failed: {request.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                UpdateStatus("Audio load failed.");
                yield break;
            }

            string json = File.ReadAllText(entry.beatDataPath);
            if (!beatPlay.LoadRuntimeBeatData(json))
            {
                UpdateStatus("Beat data parse failed.");
                yield break;
            }

            playbackAudioSource.clip = clip;
            beatPlay.PlayFromStart();
            UpdateStatus($"Playing: {entry.displayName}");
        }
    }

    private void LoadManifest()
    {
        manifest = new UserSongManifest();
        if (!File.Exists(ManifestPath))
            return;

        try
        {
            string json = File.ReadAllText(ManifestPath);
            UserSongManifest loaded = JsonUtility.FromJson<UserSongManifest>(json);
            if (loaded != null && loaded.songs != null)
                manifest = loaded;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load user song manifest: {ex.Message}", this);
        }
    }

    private void SaveManifest()
    {
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(ManifestPath, json);
    }

    private void RefreshSongsDropdown()
    {
        if (songsDropdown == null)
            return;

        songsDropdown.ClearOptions();
        List<string> labels = new List<string>();

        for (int i = 0; i < manifest.songs.Count; i++)
            labels.Add(manifest.songs[i].displayName);

        if (labels.Count == 0)
            labels.Add("No songs");

        songsDropdown.AddOptions(labels);
        songsDropdown.value = 0;
        songsDropdown.RefreshShownValue();
    }

    private UserSongManifestEntry FindEntryById(string id)
    {
        for (int i = 0; i < manifest.songs.Count; i++)
        {
            if (manifest.songs[i].id == id)
                return manifest.songs[i];
        }

        return null;
    }

    private static string ComputeSha1(byte[] bytes)
    {
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }
    }

    private void SetBusyUI()
    {
        if (importSongButton != null)
            importSongButton.interactable = false;

        if (cancelAnalysisButton != null)
            cancelAnalysisButton.gameObject.SetActive(true);
    }

    private void SetIdleUI()
    {
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
