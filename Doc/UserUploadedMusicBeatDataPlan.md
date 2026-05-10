# User Uploaded Music BeatData Plan

## Goal

Allow the player to import a local MP3 file on Android, iOS, or in the Unity Editor. The game should decode the MP3 into an `AudioClip`, run `BeatDetection` once to create pre-recorded `BeatData` JSON, save both song metadata and generated beat data locally, and then make the song selectable as background music.

This feature should not depend on `Resources` for user songs, because imported songs are runtime data and must live under `Application.persistentDataPath`.

## Current Project State

The current beat pipeline is split into two parts:

- `BeatDetection` analyzes an `AudioSource` while the clip is playing and records `BeatEvent` entries.
- `BeatDetection.SaveBeatData()` currently writes JSON only in the Unity Editor, under `Assets/Resources/BeatData`, guarded by `#if UNITY_EDITOR`.
- `BeatPlay` reads a `TextAsset` from the inspector, parses it into `BeatData`, and emits beat events while its `AudioSource` plays.
- Existing generated beat JSON lives in `Assets/Resources/BeatData`.

For user-uploaded music, this needs a runtime path:

- Import/copy MP3 into persistent storage.
- Decode the stored MP3 into an `AudioClip`.
- Analyze it and save JSON to persistent storage.
- Load the saved JSON from disk at runtime.
- Tell `BeatPlay` to use a runtime `AudioClip` and runtime `BeatData`, not just inspector-assigned `TextAsset`.

## Recommended Storage Layout

Use a dedicated folder:

```text
Application.persistentDataPath/
  UserSongs/
    songs_manifest.json
    <songId>/
      original.mp3
      beatdata.json
      cover.png optional later
```

Manifest entry:

```json
{
  "id": "sha1-or-guid",
  "displayName": "Song Name",
  "originalFileName": "Song Name.mp3",
  "audioPath": ".../UserSongs/<songId>/original.mp3",
  "beatDataPath": ".../UserSongs/<songId>/beatdata.json",
  "duration": 123.45,
  "clipFrequency": 44100,
  "analysisVersion": 1,
  "analysisSettingsHash": "..."
}
```

Use a hash of the imported file bytes for `songId` if possible. That prevents duplicate imports of the same MP3. A GUID is simpler, but duplicates become more likely.

## Platform Import Strategy

### Unity Editor

Use `EditorUtility.OpenFilePanel` for test imports inside the Editor. It gives a normal filesystem path, which is ideal for development.

Editor flow:

1. User clicks "Import MP3".
2. Editor opens native file dialog filtered to `mp3`.
3. Copy selected file into `Application.persistentDataPath/UserSongs/<songId>/original.mp3`.
4. Decode from the copied persistent path, not directly from the selected path.
5. Analyze and save `beatdata.json`.

Keep the Editor path close to mobile behavior. The selected file should still be copied into persistent storage so the rest of the pipeline is identical across platforms.

### Android

Use Android's Storage Access Framework through a Unity native file picker plugin or a small custom Android plugin.

Recommended behavior:

1. Launch an Android document picker with MIME type `audio/mpeg` or `audio/*`.
2. User selects an MP3 from Downloads, Files, cloud provider, etc.
3. Android returns a `content://` URI, not always a real filesystem path.
4. Open the URI stream in native Android code and copy bytes into `Application.persistentDataPath/UserSongs/<songId>/original.mp3`.
5. Return the copied persistent path to Unity.
6. Unity decodes the copied file with `UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG)`.

Do not design this around direct filesystem paths on Android. Modern Android storage often exposes files as content URIs. Copying to the app sandbox makes the later decode/playback/analyze pipeline consistent and avoids permission fragility.

Permissions:

- If using `ACTION_OPEN_DOCUMENT` or equivalent picker, broad storage permission should not be needed for a user-selected file.
- Avoid requesting broad storage permissions unless there is a later requirement to scan the user's music library automatically.
- For Android 13+ media permissions, only request `READ_MEDIA_AUDIO` if the app browses the media library itself. A picker-based import should avoid that.

### iOS

Use `UIDocumentPickerViewController` through a Unity native file picker plugin or a small custom iOS plugin.

Recommended behavior:

1. Open iOS document picker for `public.mp3`, `public.audio`, or equivalent UTTypes.
2. User selects a file from Files/iCloud Drive/other document providers.
3. iOS grants temporary access to the selected file.
4. Copy it immediately into `Application.persistentDataPath/UserSongs/<songId>/original.mp3`.
5. Return the copied path to Unity.
6. Unity decodes the copied file with `UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG)`.

Important iOS constraints:

- Do not rely on arbitrary filesystem access.
- Do not assume Apple Music library tracks are importable as MP3 files. DRM-protected or streaming-service tracks generally cannot be copied into the app as raw MP3.
- The supported user story should be "import an MP3 file from Files", not "pick any song from Apple Music".

## MP3 Decoding

Use Unity's `UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG)` for the first implementation.

Decode flow:

1. Convert copied persistent file path to a file URI.
2. Call `UnityWebRequestMultimedia.GetAudioClip(fileUri, AudioType.MPEG)`.
3. Extract `AudioClip` with `DownloadHandlerAudioClip.GetContent`.
4. Assign the clip to the analysis `AudioSource`.

This should be tested on:

- Unity Editor on macOS.
- Android device, not only emulator.
- iOS device, not only simulator.

Fallback plan if MP3 decoding is unreliable on a target:

- Add a native audio decode plugin per platform.
- Or transcode/import to a known supported format in native code.
- Or restrict supported formats after testing.

## Beat Analysis Runtime Changes

The current `BeatDetection` analyzes in real time while the `AudioSource` plays. That means a 3 minute song takes about 3 minutes to analyze unless the analyzer is rewritten to process raw samples offline.

Recommended first version: real-time analysis with progress UI.

Required changes:

1. Add a runtime analysis API around `BeatDetection`.
2. Allow `BeatDetection` to return or expose `BeatData` when complete.
3. Allow runtime JSON save outside the Editor.
4. Add progress reporting based on `audioSource.time / clip.length`.
5. Allow muted analysis, so the user does not need to hear the song during import.

Implementation shape:

```text
UserSongImportService
  PickFile()
  CopyToPersistentStorage()
  DecodeAudioClip()
  AnalyzeClip()
  SaveBeatDataJson()
  SaveManifest()

BeatDetection
  AnalyzeClip(AudioClip clip, AnalysisOptions options)
  event OnAnalysisProgress(float progress)
  event OnAnalysisComplete(BeatData data)
  event OnAnalysisFailed(string error)
```

`BeatDetection.SaveBeatData()` should be split:

- Editor asset export path for prebuilt songs.
- Runtime JSON string/file output for user songs.

Do not use `AssetDatabase` in runtime code.

## Playback Runtime Changes

`BeatPlay` currently expects `TextAsset preRecordedBeatData`. User songs need a runtime load path.

Add APIs like:

```csharp
public void LoadRuntimeBeatData(string json)
public void LoadRuntimeBeatData(BeatData data)
public void SetRuntimeSong(AudioClip clip, BeatData beatData)
```

Playback flow:

1. User selects imported song from song list.
2. Load `original.mp3` from persistent storage into an `AudioClip`.
3. Read `beatdata.json` from persistent storage.
4. Parse `BeatData`.
5. Set `AudioSource.clip`.
6. Set `BeatPlay` runtime beat data.
7. Call `PlayFromStart()`.

Keep existing `TextAsset` behavior for built-in songs.

## UI Flow

Recommended user-facing states:

1. `Import Song`
2. File picker opens.
3. `Copying song...`
4. `Loading audio...`
5. `Analyzing beats... 0-100%`
6. `Song ready`
7. Song appears in background music selection list.

Failure states:

- Unsupported or unreadable file.
- Decode failed.
- Song too long.
- Not enough storage.
- Analysis interrupted.
- Beat data save failed.

Add a maximum song duration for mobile. A practical first limit is 8-10 minutes. This avoids long analysis sessions and large memory use.

## Mobile Performance and UX

Real-time analysis has predictable cost but slow UX. For a first version it is acceptable if the import UI is explicit: "Analyzing song, this may take the length of the track."

Consider these safeguards:

- Mute the analysis `AudioSource` or route it to a muted mixer group.
- Prevent device sleep during analysis, then restore normal sleep settings.
- Cancel analysis if the app is backgrounded.
- Save an `analysisVersion` and settings hash. If BeatDetection settings change later, mark old beat data for re-analysis.
- Avoid analyzing while gameplay is running.

## Future Offline Analyzer

The better long-term implementation is an offline sample analyzer:

- Decode MP3 to PCM samples.
- Process sample windows manually.
- Run the same energy/frequency beat logic without playing through an `AudioSource`.
- Generate `BeatData` faster than real time if CPU allows.

This requires refactoring `BeatDetection` because it currently depends on:

- `AudioSource.GetSpectrumData`
- `AudioSource.GetOutputData`
- `audioSource.timeSamples`
- playback ending to trigger save

The offline analyzer is more work but better for UX and batch imports.

## Suggested Implementation Order

1. Add `UserSongManifest` data model and persistent storage layout.
2. Add Editor-only MP3 picker using `EditorUtility.OpenFilePanel`.
3. Add MP3 copy-to-persistent-storage path.
4. Add `RuntimeAudioClipLoader` using `UnityWebRequestMultimedia.GetAudioClip`.
5. Refactor `BeatDetection` so runtime code can receive `BeatData` and save JSON without `AssetDatabase`.
6. Add analysis progress/cancel UI.
7. Add runtime `BeatPlay` loading API for `BeatData` and `AudioClip`.
8. Add Android native picker or file picker plugin.
9. Add iOS document picker or file picker plugin.
10. Test on physical Android and iOS devices with short, medium, and long MP3 files.

## Recommended Plugin Decision

Use a proven Unity native file picker plugin unless there is a strong reason to write native platform code.

Needed plugin capabilities:

- Android Storage Access Framework / document picker.
- iOS document picker.
- Returns selected file bytes or copies selected file to an app-accessible path.
- Works with Unity 6 and IL2CPP.
- Supports MIME/UTType filtering for MP3/audio.

If a plugin returns a `content://` URI on Android, still copy the data to `persistentDataPath` before decoding.

## Acceptance Criteria

- Editor can import an MP3 through a file dialog.
- Android can import an MP3 through the system picker.
- iOS can import an MP3 through the Files picker.
- Imported MP3 is copied into persistent app storage.
- Beat data JSON is generated and saved next to the song.
- The song appears in the game's music list after analysis.
- On next app launch, the song remains available.
- Playback uses the imported `AudioClip` plus generated `BeatData`.
- Built-in music using Resources still works.

## References

- Unity `UnityWebRequestMultimedia.GetAudioClip`: https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequestMultimedia.GetAudioClip.html
- Unity `AudioType`: https://docs.unity3d.com/ScriptReference/AudioType.html
- Unity `Application.persistentDataPath`: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
- Unity `EditorUtility.OpenFilePanel`: https://docs.unity3d.com/ScriptReference/EditorUtility.OpenFilePanel.html
