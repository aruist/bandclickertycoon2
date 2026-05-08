using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Audio;

public class pELiNhaLLitSIJa : MonoBehaviour {
    #if SOFTCEN_DEBUG
    public const string TAG = "SCPelinH ";
    #endif
    public AudioMixer mainAudioMixer;
    public static pELiNhaLLitSIJa Instance;
    //public static event Action OnMoneyChanged;
    public static event Action OnMultiplyChanged;
    public static event Action OnGameLoaded;

    public ajat mAjat;
    public keRrOIn Kerroin;

    public int _multiply = 0;
    public int Multiply
    {
        get { return _multiply; }
        set
        {
            if (value > 2)
                _multiply = 0;
            else
                _multiply = value;
            if (OnMultiplyChanged != null)
            {
                OnMultiplyChanged();
            }
        }
    }
    public int getMultiplyCount()
    {
        if (_multiply == 1)
            return 10;
        if (_multiply == 2)
            return 50;
        return 1;
    }
    // public double money
    // {
    //     get { return playerData.Money; }
    // }

    public PlayerData playerData = null;
    private const string SaveFileName = "bandclicker_save.json";
    private bool isGameLoaded;

    public bool IsGameLoaded => isGameLoaded;

    private string SaveFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
    }

    // Use this for initialization
    void Awake()
    {
        if (Instance == null)
        {
            isGameLoaded = false;
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }

    }

    void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        #if SOFTCEN_DEBUG
        UnityEngine.Debug.Log("Audio volume: " + playerData._aanivoluumi + ", Music volume: " + playerData._musiikkivoluumi);
        #endif
        SetSFXVolume(playerData._aanivoluumi);
        SetMusicVolume(playerData._musiikkivoluumi);

    }

    void OnApplicationQuit()
    {
        Save();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Save();
        }
    }

    private float secTimer = 0;
    void Update()
    {
        secTimer += Time.deltaTime;
        if (secTimer >= 1f)
        {
            secTimer = secTimer - 1f;
            if (Kerroin.KerroinArvo > 0)
            {
                if (Kerroin.anNAJaljellaOLEvaAIka() <= 0)
                {
                    Kerroin.noLLaaKerroin();
                    TallennaKerroin();
                }
            }
        }

        // #if SOFTCEN_DEBUG
        // if (Input.GetKeyUp(KeyCode.L))
        // {
        //     ChangeMoney(10000000);
        //     UnityEngine.Debug.Log(TAG + "Money: " + money);
        // }
        // else if (Input.GetKeyUp(KeyCode.K))
        // {
        //     ChangeMoney(1000);
        //     UnityEngine.Debug.Log(TAG + "Money: " + money);
        // }
        // else if (Input.GetKeyUp(KeyCode.M))
        // {
        //     ChangeMoney(1000000);
        //     UnityEngine.Debug.Log(TAG + "Money: " + money);
        // }
        // else if (Input.GetKeyUp(KeyCode.B))
        // {
        //     ChangeMoney(Math.Pow(10, 9));
        //     UnityEngine.Debug.Log(TAG + "Money: " + money);
        // }
        // #endif
    }

    // TODO: No needed remove all references
    // public void ChangeMoney(double amount)
    // {
    //     playerData.ChangeMoney(amount);
    // }

    void OnDestroy()
    {
        // UnityEngine.Debug.Log(TAG + "OnDestroy");
    }

    public void ExitGame()
    {
        Save();
        mAiNOsPomO.instance.HideBanner();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_ANDROID
        System.Diagnostics.ProcessThreadCollection pt = System.Diagnostics.Process.GetCurrentProcess().Threads;
        foreach (System.Diagnostics.ProcessThread p in pt)
        {
            #if SOFTCEN_DEBUG
            UnityEngine.Debug.Log(TAG + "Exit p: " + p.Id.ToString());
            #endif
        }
		Application.Quit();
        #else
		Application.Quit();
        #endif
    }

    public void Save()
    {
        WriteSaveFile(updateLastUsedTicks: true);
    }
    public void Load()
    {
        playerData = new PlayerData();
        playerData.Init();
        Kerroin = new keRrOIn();

        if (!File.Exists(SaveFilePath))
        {
            #if SOFTCEN_DEBUG
            UnityEngine.Debug.Log(TAG + "Load() no save file: " + SaveFilePath);
            #endif
            OnGameLoaded?.Invoke();
            isGameLoaded = true;
            return;
        }

        try
        {
            string json = File.ReadAllText(SaveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            if (saveData == null)
            {
                OnGameLoaded?.Invoke();
                isGameLoaded = true;
                return;
            }

            if (saveData.playerData != null)
                ApplyPlayerData(saveData.playerData, playerData);

            ApplyKerroinData(saveData.kerroin);
            #if SOFTCEN_DEBUG
            Debug.Log($"Load() done: {json}");
            #endif

        }
        catch
        {
            playerData = new PlayerData();
            playerData.Init();
            Kerroin = new keRrOIn();
        }
        OnGameLoaded?.Invoke();
        isGameLoaded = true;
    }

    public int PurchaseUpgradeItem(int placeId, int itemId, bool callEvent)
    {
        return playerData.PurchaseItem(placeId, itemId, callEvent);
    }

    public bool TallennaKerroin()
    {
        if (Kerroin == null || !Kerroin.Muutettu)
            return true;

        return WriteSaveFile(updateLastUsedTicks: false);
    }

    private bool WriteSaveFile(bool updateLastUsedTicks)
    {
        if (playerData == null)
            return false;

        if (updateLastUsedTicks)
            playerData._lastusedTicks = DateTime.UtcNow.Ticks;
        #if SOFTCEN_DEBUG
        Debug.Log($"WriteSaveFile playerData: {playerData}");
        #endif
        GameSaveData saveData = BuildSaveData();
        #if SOFTCEN_DEBUG
        Debug.Log($"WriteSaveFile saveData: {saveData}");
        #endif
        string json = JsonUtility.ToJson(saveData, true);
        string tempPath = SaveFilePath + ".tmp";
        #if SOFTCEN_DEBUG
        Debug.Log($"WriteSaveFile json: {json}");
        #endif

        try
        {
            File.WriteAllText(tempPath, json);
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
            File.Move(tempPath, SaveFilePath);

            if (Kerroin != null)
                Kerroin.Muutettu = false;
            playerData._changed = false;

            #if SOFTCEN_DEBUG
            UnityEngine.Debug.Log(TAG + "WriteSaveFile(): " + SaveFilePath);
            #endif
            return true;
        }
        catch
        {
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch { }
            return false;
        }
    }

    private GameSaveData BuildSaveData()
    {
        GameSaveData saveData = new GameSaveData();
        saveData.version = 1;
        saveData.playerData = BuildPlayerDataSave(playerData);
        saveData.kerroin = BuildKerroinSave(Kerroin);
        saveData.savedAtTicks = DateTime.UtcNow.Ticks;
        return saveData;
    }

    private static PlayerDataSave BuildPlayerDataSave(PlayerData source)
    {
        PlayerDataSave data = new PlayerDataSave();
        if (source == null)
            return data;

        data.moneyIterations = source._moneyIterations;
        data.earnedMoney = source._earnedMoney;
        data.usedMoney = source._usedMoney;
        data.lastUsedTicks = source._lastusedTicks;
        data.signInOnStartup = source._signInOnStartup;
        data.sfxVolume = source._aanivoluumi;
        data.musicVolume = source._musiikkivoluumi;
        data.activeShareHolders = source._activeShareHolders;

        data.places = new List<PlaceSaveData>();
        if (source.places != null)
        {
            foreach (KeyValuePair<int, PlaceData> kvp in source.places)
            {
                PlaceData place = kvp.Value;
                if (place == null)
                    continue;

                PlaceSaveData placeData = new PlaceSaveData();
                placeData.id = place.id;
                placeData.kassaKaappiKoko = place.kassaKaappiKoko;
                placeData.vaultMoney = place.vaultMoney;
                placeData.money = place.money;
                placeData.items = new List<PlaceItemSaveData>();

                if (place.item != null)
                {
                    foreach (KeyValuePair<int, int> itemKvp in place.item)
                    {
                        PlaceItemSaveData itemData = new PlaceItemSaveData();
                        itemData.key = itemKvp.Key;
                        itemData.value = itemKvp.Value;
                        placeData.items.Add(itemData);
                    }
                }

                data.places.Add(placeData);
            }
        }

        return data;
    }

    private static KerroinSaveData BuildKerroinSave(keRrOIn source)
    {
        KerroinSaveData data = new KerroinSaveData();
        if (source == null)
            return data;

        data.versio = source.versio;
        data.value = source.KerroinArvo;
        data.startTicks = source.AloitusAika;
        data.duration = source.Kesto;
        return data;
    }

    private static void ApplyPlayerData(PlayerDataSave source, PlayerData target)
    {
        if (source == null || target == null)
            return;

        target._moneyIterations = source.moneyIterations;
        target._earnedMoney = source.earnedMoney;
        target._usedMoney = source.usedMoney;
        target._lastusedTicks = source.lastUsedTicks;
        target._signInOnStartup = source.signInOnStartup;
        target._aanivoluumi = source.sfxVolume;
        target._musiikkivoluumi = source.musicVolume;
        target._activeShareHolders = source.activeShareHolders;
        target._changed = false;

        if (target.places == null)
            target.places = new Dictionary<int, PlaceData>();
        else
            target.places.Clear();

        if (source.places == null)
            return;

        for (int i = 0; i < source.places.Count; i++)
        {
            PlaceSaveData srcPlace = source.places[i];
            if (srcPlace == null)
                continue;

            PlaceData place = new PlaceData(srcPlace.id);
            place.kassaKaappiKoko = srcPlace.kassaKaappiKoko;
            place.vaultMoney = srcPlace.vaultMoney;
            place.money = new SecureDoubleLight(srcPlace.money);

            if (srcPlace.items != null)
            {
                for (int j = 0; j < srcPlace.items.Count; j++)
                {
                    PlaceItemSaveData srcItem = srcPlace.items[j];
                    if (srcItem != null)
                        place.item[srcItem.key] = srcItem.value;
                }
            }

            target.places[place.id] = place;
        }
    }

    private void ApplyKerroinData(KerroinSaveData source)
    {
        if (source == null)
        {
            Kerroin = new keRrOIn();
            return;
        }

        Kerroin = new keRrOIn();
        Kerroin.versio = source.versio;
        if (source.value > 0 && source.duration > 0 && source.startTicks > 0)
            Kerroin.aSEtaKErroIN(source.value, source.startTicks, source.duration);
        else
            Kerroin.alusta();
        Kerroin.Muutettu = false;
    }

    [Serializable]
    private class GameSaveData
    {
        public int version;
        public long savedAtTicks;
        public PlayerDataSave playerData;
        public KerroinSaveData kerroin;
    }

    [Serializable]
    private class PlayerDataSave
    {
        public double moneyIterations;
        public double earnedMoney;
        public double usedMoney;
        public long lastUsedTicks;
        public bool signInOnStartup;
        public float sfxVolume;
        public float musicVolume;
        public int activeShareHolders;
        public List<PlaceSaveData> places;
    }

    [Serializable]
    private class PlaceSaveData
    {
        public int id;
        public int kassaKaappiKoko;
        public double vaultMoney;
        public double money;
        public List<PlaceItemSaveData> items;
    }

    [Serializable]
    private class PlaceItemSaveData
    {
        public int key;
        public int value;
    }

    [Serializable]
    private class KerroinSaveData
    {
        public int versio;
        public double value;
        public long startTicks;
        public long duration;
    }

    public void SetSFXVolume(float volume)
    {
        mainAudioMixer.SetFloat("SFXVol", volume);
        playerData._aanivoluumi = volume;

    }
    public void SetMusicVolume(float volume)
    {
        mainAudioMixer.SetFloat("MusicVol", volume);
        playerData._musiikkivoluumi = volume;
    }

    public void ChangeRegionVaultMoney(int id, double amount)
    {
        if (playerData == null) return;
        playerData.ChangeRegionValutMoney(id, amount);
    }

    public double GetRegionVaultMoney(int id)
    {
        if (playerData == null) return 0d;
        return playerData.GetRegionVaultMoney(id);
    }

    public int GetVaultOwned(int id)
    {
        if (playerData == null) return 0;
        return playerData.GetVaultOwned(id);
    }

    public void ChangeRegionMoney(int id, double amount)
    {
        if (playerData == null) return;
        playerData.ChangeRegionMoney(id, amount);
    }

    public double GetRegionMoney(int id)
    {
        if (playerData == null) return 0d;
        return playerData.GetRegionMoney(id);
    }

}
