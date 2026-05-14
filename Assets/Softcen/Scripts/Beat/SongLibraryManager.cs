using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SongLibraryEntry
{
    public string songId;
    public string displayName;
    public string sourceType; // Original, UserImported
    public string audioPath;
    public string beatDataPath;
    public string audioHash;
    public int beatDataVersion;
    public float duration;
    public int clipFrequency;
    public string originalFileName;
}

[Serializable]
public class SongLibraryManifest
{
    public int schemaVersion = 1;
    public int analysisVersion = PCMBeatDetection.AnalysisVersion;
    public List<SongLibraryEntry> songs = new List<SongLibraryEntry>();
}

public sealed class SongLibraryManager : MonoBehaviour
{
    [Serializable]
    private class OriginalSongDefinition
    {
        public string songId;
        public string displayName;
        public AudioClip audioClip;
        public TextAsset beatDataJson;
    }

    public static SongLibraryManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PCMBeatDetection pcmBeatDetection;
    [SerializeField] private BeatPlay defaultBeatPlay;

    [Header("Original Songs")]
    [SerializeField] private List<OriginalSongDefinition> originalSongs = new List<OriginalSongDefinition>();
    [SerializeField] private BeatPlay beatPlay;

    public event Action OnLibraryChanged;
    public event Action<string, float> OnAnalysisProgress;
    public event Action<string> OnAnalysisFailed;

    public int CurrentAnalysisVersion => PCMBeatDetection.AnalysisVersion;
    public IReadOnlyList<SongLibraryEntry> Songs => manifest.songs;

    private SongLibraryManifest manifest = new SongLibraryManifest();
    private Coroutine activeImportCoroutine;
    private string activeSongId;

    private string LibraryRoot => Path.Combine(Application.persistentDataPath, "SongLibrary");
    private string AudioRoot => Path.Combine(LibraryRoot, "Audio");
    private string BeatDataRoot => Path.Combine(LibraryRoot, "BeatData");
    private string ManifestPath => Path.Combine(LibraryRoot, "Manifest.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (pcmBeatDetection == null)
            pcmBeatDetection = GetComponent<PCMBeatDetection>();

        if (pcmBeatDetection == null)
            pcmBeatDetection = gameObject.AddComponent<PCMBeatDetection>();

        Directory.CreateDirectory(LibraryRoot);
        Directory.CreateDirectory(AudioRoot);
        Directory.CreateDirectory(BeatDataRoot);

        LoadManifest();
        RegisterOriginalSongs();
    }

    public void RegisterBeatPlay(BeatPlay play)
    {
        beatPlay = play;
        defaultBeatPlay = play;
    }

    public SongLibraryEntry FindSong(string songId)
    {
        for (int i = 0; i < manifest.songs.Count; i++)
        {
            if (manifest.songs[i].songId == songId)
                return manifest.songs[i];
        }

        return null;
    }

    public Coroutine ImportSong(string sourcePath, Action<bool, string, SongLibraryEntry> onFinished)
    {
        if (activeImportCoroutine != null)
        {
            onFinished?.Invoke(false, "Another import/analysis is already running.", null);
            return null;
        }

        activeImportCoroutine = StartCoroutine(ImportSongRoutine(sourcePath, onFinished));
        return activeImportCoroutine;
    }

    public void CancelCurrentAnalysis()
    {
        pcmBeatDetection.CancelAnalysis();
    }

    public Coroutine LoadAndPlaySong(string songId, Action<bool, string> onFinished = null)
    {
        return StartCoroutine(LoadAndPlaySongRoutine(songId, beatPlay != null ? beatPlay : defaultBeatPlay, onFinished));
    }

    public bool IsBeatDataStale(SongLibraryEntry entry)
    {
        if (entry == null)
            return true;

        if (entry.beatDataVersion < CurrentAnalysisVersion)
            return true;

        if (string.IsNullOrWhiteSpace(entry.beatDataPath) || !File.Exists(entry.beatDataPath))
            return true;

        string json = File.ReadAllText(entry.beatDataPath);
        if (string.IsNullOrWhiteSpace(json))
            return true;

        BeatData beatData = JsonUtility.FromJson<BeatData>(json);
        if (beatData == null || beatData.beatEvents == null || beatData.beatEvents.Count == 0)
            return true;

        return beatData.analysisVersion < CurrentAnalysisVersion;
    }

    private IEnumerator ImportSongRoutine(string sourcePath, Action<bool, string, SongLibraryEntry> onFinished)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            FinishImport(onFinished, false, "Song file not found.", null);
            yield break;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(sourcePath);
        }
        catch (Exception ex)
        {
            FinishImport(onFinished, false, $"Could not read source file: {ex.Message}", null);
            yield break;
        }

        string hash = ComputeSha1(bytes);
        string songId = hash;
        string audioPath = Path.Combine(AudioRoot, $"{songId}.mp3");
        string beatDataPath = Path.Combine(BeatDataRoot, $"{songId}.json");

        try
        {
            File.WriteAllBytes(audioPath, bytes);
        }
        catch (Exception ex)
        {
            FinishImport(onFinished, false, $"Could not save copied song: {ex.Message}", null);
            yield break;
        }

        SongLibraryEntry entry = FindSong(songId);
        if (entry == null)
        {
            entry = new SongLibraryEntry();
            manifest.songs.Add(entry);
        }

        entry.songId = songId;
        entry.displayName = Path.GetFileNameWithoutExtension(sourcePath);
        entry.sourceType = "UserImported";
        entry.audioPath = audioPath;
        entry.beatDataPath = beatDataPath;
        entry.audioHash = hash;
        entry.originalFileName = Path.GetFileName(sourcePath);

        yield return AnalyzeSongIfNeededRoutine(entry, true);
        SaveManifest();
        OnLibraryChanged?.Invoke();

        if (IsBeatDataStale(entry))
            FinishImport(onFinished, false, "Analysis failed.", null);
        else
            FinishImport(onFinished, true, "Song imported.", entry);
    }

    private IEnumerator LoadAndPlaySongRoutine(string songId, BeatPlay beatPlay, Action<bool, string> onFinished)
    {
        if (beatPlay == null)
        {
            onFinished?.Invoke(false, "BeatPlay reference missing.");
            yield break;
        }

        SongLibraryEntry entry = FindSong(songId);
        if (entry == null)
        {
            onFinished?.Invoke(false, "Song not found.");
            yield break;
        }

        if (entry.sourceType == "Original")
        {
            OriginalSongDefinition original = FindOriginalSong(songId);
            if (original == null || original.audioClip == null || original.beatDataJson == null)
            {
                onFinished?.Invoke(false, "Original song assets missing.");
                yield break;
            }

            if (!beatPlay.LoadRuntimeBeatData(original.beatDataJson.text))
            {
                onFinished?.Invoke(false, "Could not parse original beat data.");
                yield break;
            }

            if (!beatPlay.SetRuntimeSong(original.audioClip, beatPlay.LoadedBeatData))
            {
                onFinished?.Invoke(false, "Could not set original song.");
                yield break;
            }

            beatPlay.PlayFromStart();
            onFinished?.Invoke(true, $"Playing: {entry.displayName}");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(entry.audioPath) || !File.Exists(entry.audioPath))
        {
            onFinished?.Invoke(false, "Song audio file missing.");
            yield break;
        }

        yield return AnalyzeSongIfNeededRoutine(entry, false);
        if (IsBeatDataStale(entry))
        {
            onFinished?.Invoke(false, "Beat data is missing or outdated and could not be rebuilt.");
            yield break;
        }

        string uri = new Uri(Path.GetFullPath(entry.audioPath)).AbsoluteUri;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onFinished?.Invoke(false, $"Audio load failed: {request.error}");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                onFinished?.Invoke(false, "Audio load failed.");
                yield break;
            }

            string json = File.ReadAllText(entry.beatDataPath);
            BeatData beatData = JsonUtility.FromJson<BeatData>(json);
            if (beatData == null || beatData.beatEvents == null)
            {
                onFinished?.Invoke(false, "Beat data parse failed.");
                yield break;
            }

            if (!beatPlay.SetRuntimeSong(clip, beatData))
            {
                onFinished?.Invoke(false, "Could not set runtime song.");
                yield break;
            }

            beatPlay.PlayFromStart();
            onFinished?.Invoke(true, $"Playing: {entry.displayName}");
        }
    }

    private IEnumerator AnalyzeSongIfNeededRoutine(SongLibraryEntry entry, bool force)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.audioPath) || !File.Exists(entry.audioPath))
            yield break;

        if (!force && !IsBeatDataStale(entry))
            yield break;

        activeSongId = entry.songId;
        pcmBeatDetection.AnalyzeMp3File(entry.audioPath, entry.beatDataPath);

        while (pcmBeatDetection.IsRunning)
        {
            float progress = Mathf.Clamp01(pcmBeatDetection.ProgressPercentage / 100f);
            OnAnalysisProgress?.Invoke(activeSongId, progress);
            yield return null;
        }

        if (!pcmBeatDetection.IsComplete || !string.IsNullOrEmpty(pcmBeatDetection.ErrorMessage))
        {
            OnAnalysisFailed?.Invoke(string.IsNullOrWhiteSpace(pcmBeatDetection.ErrorMessage) ? "Analysis failed." : pcmBeatDetection.ErrorMessage);
            yield break;
        }

        BeatData beatData = pcmBeatDetection.LastBeatData;
        if (beatData == null)
            yield break;

        entry.duration = beatData.audioLength;
        entry.clipFrequency = beatData.clipFrequency;
        entry.beatDataVersion = beatData.analysisVersion;
        manifest.analysisVersion = CurrentAnalysisVersion;
    }

    private void FinishImport(Action<bool, string, SongLibraryEntry> onFinished, bool success, string message, SongLibraryEntry entry)
    {
        activeSongId = null;
        activeImportCoroutine = null;
        onFinished?.Invoke(success, message, entry);
    }

    private OriginalSongDefinition FindOriginalSong(string songId)
    {
        for (int i = 0; i < originalSongs.Count; i++)
        {
            if (originalSongs[i] != null && originalSongs[i].songId == songId)
                return originalSongs[i];
        }

        return null;
    }

    private void RegisterOriginalSongs()
    {
        bool changed = false;
        for (int i = 0; i < originalSongs.Count; i++)
        {
            OriginalSongDefinition original = originalSongs[i];
            if (original == null || string.IsNullOrWhiteSpace(original.songId))
                continue;

            SongLibraryEntry entry = FindSong(original.songId);
            if (entry == null)
            {
                entry = new SongLibraryEntry();
                manifest.songs.Add(entry);
                changed = true;
            }

            entry.songId = original.songId;
            entry.displayName = string.IsNullOrWhiteSpace(original.displayName) ? original.songId : original.displayName;
            entry.sourceType = "Original";
            entry.beatDataVersion = CurrentAnalysisVersion;
            entry.duration = original.audioClip != null ? original.audioClip.length : 0f;
            entry.clipFrequency = original.audioClip != null ? original.audioClip.frequency : 0;
        }

        if (changed)
        {
            SaveManifest();
            OnLibraryChanged?.Invoke();
        }
    }

    private void LoadManifest()
    {
        manifest = new SongLibraryManifest();

        if (!File.Exists(ManifestPath))
            return;

        try
        {
            string json = File.ReadAllText(ManifestPath);
            SongLibraryManifest loaded = JsonUtility.FromJson<SongLibraryManifest>(json);
            if (loaded != null && loaded.songs != null)
                manifest = loaded;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Could not load song library manifest: {ex.Message}", this);
        }

        manifest.analysisVersion = CurrentAnalysisVersion;
    }

    private void SaveManifest()
    {
        manifest.analysisVersion = CurrentAnalysisVersion;
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(ManifestPath, json);
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
}
