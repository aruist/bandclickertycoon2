using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using OneP.InfinityScrollView;
using System;
using WhiteCat.Paths;
using UnityEngine.Audio;
using System.Collections;
using TMPro;

public class paIkKaHaLlItSiJa : MonoBehaviour {
    public static paIkKaHaLlItSiJa Instance = null;
    public static event Action OnRegionChanged;

    [SerializeField] private WorldDefinition worldDefinition;

    public static event Action<int, int, int> onImprovePurchased;
    [SerializeField] private MoveAlongPathWithSpeed camMoveAlongPath;
    [SerializeField] private Transform camTransform;

    [SerializeField] private ObjectPooler poolCoinCollect;
    [SerializeField] private Transform trCoinCollect;

    //[SerializeField] private TextMeshProUGUI txtNyKYInenRAta;
    // [SerializeField] private TextMeshProUGUI txtCollectedMoney;
    // [SerializeField] private float animationDurationSec;
    // [SerializeField] private AnimatedNumberText animatedNumberText;

    [SerializeField] private InfinityScrollView scrollView;

    [SerializeField] private Dictionary<int, PlaceItem> placeDictionary;
    // public List<PlaceItem> places;
    [SerializeField] private int currentPlaceId;
    [SerializeField] private PlaceItem currentPlace;
    [SerializeField] private GameObject goNyKyiNENPaikka;
    private Place paikkaObjekti;

    [SerializeField] private Button btnNext;
    [SerializeField] private Button btnPrev;
    [SerializeField] private vaIHToPaNeeLi vaihtoPaneeli;

    public WorldState World { get; private set; }
    private bool useWorldDefinitionMotor;
    private readonly int[] starMilestones = { 25, 50, 100, 150, 200 };
    public bool UsesWorldDefinitionMotor => useWorldDefinitionMotor;

    private RegionState currentRegionState;
    public RegionState CurrentRegionState => currentRegionState;

    private bool gigStarted;
    public bool GigStarted => gigStarted;

    public enum improvementProgress
    {
        OPASTUS,
        STEP1,
        STEP2,
        STEP3
    }

    private enum paikkaTila
    {
        UNINITIALIZED,
        NORMAALI,
        ALOITA,
        LATAA,
        VALMIS
    }

    private paikkaTila tila;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
			return;
        }
        SetTila(paikkaTila.UNINITIALIZED);
        if (pELiNhaLLitSIJa.Instance != null && pELiNhaLLitSIJa.Instance.IsGameLoaded)
        {
            OnGameLoaded();
        }

    }

    void OnEnable()
    {
        pELiNhaLLitSIJa.OnGameLoaded += OnGameLoaded;
        PlaceItem.OnCollectedMoneyChanged += PlaceItem_OnCollectedMoneyChanged_Legacy;
        MoneyCollectedPanel.OnCollectedMoneyPressed += MoneyCollectedPanel_OnCollectedMoneyPressed;
        vaIHToPaNeeLi.OnVaihtoValmis += VaIHToPaNeeLi_OnVaihtoValmis;
    }

    void OnDisable()
    {
        pELiNhaLLitSIJa.OnGameLoaded -= OnGameLoaded;
        PlaceItem.OnCollectedMoneyChanged -= PlaceItem_OnCollectedMoneyChanged_Legacy;
        MoneyCollectedPanel.OnCollectedMoneyPressed -= MoneyCollectedPanel_OnCollectedMoneyPressed;
        vaIHToPaNeeLi.OnVaihtoValmis -= VaIHToPaNeeLi_OnVaihtoValmis;
    }

    private void OnGameLoaded()
    {
        // animatedNumberText.Bind(txtCollectedMoney, animationDurationSec);
        useWorldDefinitionMotor = worldDefinition != null;
        if (useWorldDefinitionMotor)
        {
            World = new WorldState(worldDefinition);
            InitializeWorldDefinitionMotor();
        }
        SetTila(paikkaTila.ALOITA);
        currentPlaceId = 0;
        // OnGameLoadedLegacy();
    }

    void Start()
    {
        //StartCoroutine(InitializeWorldDefinitionMotorDelayed());
    }

    void Update()
    {
        if (tila == paikkaTila.UNINITIALIZED) return;

        if (useWorldDefinitionMotor)
        {
            TickWorldDefinitionMotor(Time.deltaTime, syncUIData: false);
        }

        if (tila == paikkaTila.NORMAALI)
            return;

        if (tila == paikkaTila.ALOITA)
        {
            SetTila(paikkaTila.LATAA);
            StartCoroutine(LoadLevel());
        }
        else if (tila == paikkaTila.VALMIS)
        {
            UpdateRegion();
            vaihtoPaneeli.haivyta();
            SetTila(paikkaTila.NORMAALI);
            OnRegionChanged?.Invoke();
        }

    }

    public ImprovementDefinition GetCurrentRegionImprovement(int index)
    {
        if (currentRegionState == null) return null;
        if (index >= currentRegionState.Improvements.Count) return null;
        return currentRegionState.Improvements[index].Definition;
    }

    private void UpdateRegion()
    {
        currentRegionState = GetRegion(currentPlaceId);
        UpdateRegionTitle();
        if (scrollView != null) scrollView.Setup(10);

        // Legacy:
        // paivitaRata_Legacy();
        // //currentPlace = getCurrentPlace_Legacy();
        // PlaceItem_OnCollectedMoneyChanged_Legacy();
        // PaiVITaRATaTieDOT_Legacy();
        // if (currentPlace != null) {
        //     Vector3 pos = scrollView.content.position;
        //     pos.y = currentPlace.posY;
        //     //pos.y = places[currentPlaceId].posY;
        //     scrollView.content.position = pos;
        //     scrollView.Setup(10);
        // }
        // paIVitANapiT_Legacy(0);
        // Setup campath
        // camMoveAlongPath.path = paikkaObjekti.camPath;
        // camMoveAlongPath.speed = paikkaObjekti.camSpeed;
        // camMoveAlongPath.distance = 0f;
        // camMoveAlongPath.AddMovingObject(paikkaObjekti.keyFrameList, camTransform);
        //camMoveAlongPath.Sync();

    }

    private RegionState GetRegion(int id)
    {
        if (World == null || World.Regions == null) return null;
        for (int i = 0; i < World.Regions.Count; i++)
        {
            RegionState region = World.Regions[i];
            if (region == null) continue;
            if (region.Definition != null && region.Definition.id == id) return region;
        }

        return null;
    }

    private void VaIHToPaNeeLi_OnVaihtoValmis()
    {
        SetTila(paikkaTila.ALOITA);
    }

    private void MoneyCollectedPanel_OnCollectedMoneyPressed()
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || currentRegionState == null) return;
        double amount = currentRegionState.GetVaultMoney();
        if (amount > 0)
        {
            SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_PICKUPCOINS);
            GameObject go = poolCoinCollect.GetPooledObject();
            go.transform.position = trCoinCollect.position;
            go.SetActive(true);
            currentRegionState.AddMoney(amount);
            currentRegionState.DecreaseVaultMoney(amount);
        }
    }

    public double GetCurrentRegionValutMoney()
    {
        if (currentRegionState == null) return 0;
        return currentRegionState.GetVaultMoney();
    }

    public int GetCurrentRegionStars()
    {
        if (currentRegionState == null) return 0;
        return currentRegionState.Stars;
    }

    public double GetCurrentRegionMoney()
    {
        if (currentRegionState == null) return 0;
        return currentRegionState.GetMoney();
    }

    public double GetMoney(int id)
    {
        if (World == null) return 0;
        return World.GetRegionValutMoney(id);
        // if (placeDictionary == null) return 0;
        // if (placeDictionary.ContainsKey(id))
        // {
        //     return placeDictionary[id].Money;
        // }
        // return 0;
    }

    public ImprovementState GetImprovementState(int index)
    {
        if (index < 0 || World == null || World.Regions == null) return null;
        for (int i=0; i < World.Regions.Count; i++)
        {
            RegionState region = World.Regions[i];
            if (region == null || region.Definition == null) continue;
            if (region.Definition.id == currentPlaceId)
            {
                if (region.Improvements == null) continue;
                if (index >= region.Improvements.Count) return null;
                return region.Improvements[index];
            }
        }
        return null;
    }

	public int GetLevel(int placeId, int improvementId) {
        if (UseWorldMotorForPlace(placeId) && World != null)
        {
            if (placeId >= 0 && placeId < World.Regions.Count)
            {
                RegionState region = World.Regions[placeId];
                Debug.Log($"GetLevel: placeId: {placeId}, improvementId: {improvementId}, level: {region.Improvements[improvementId].Level}");
                if (region != null && improvementId >= 0 && improvementId < region.Improvements.Count)
                    return region.Improvements[improvementId].Level;
            }
            return 0;
        }

        // Legacy:
        // if (placeDictionary.TryGetValue(placeId, out PlaceItem item))
        // {
        //     if (item.improvements != null && improvementId >= 0 && improvementId < item.improvements.Count)
        //     {
        //         return item.improvements[improvementId].level;
        //     }
        // }
		return 0;
	}

    private void TryToStartGig()
    {

    }

    public void PurchaseImprovement(int index, ImprovementItemUI uiItem)
    {
        if (UseWorldMotorForPlace(currentPlaceId))
        {
            int purchasedLevel;
            bool success = TryPurchaseWorldImprovement(currentPlaceId, index, 1, out purchasedLevel, out _);
            if (success && onImprovePurchased != null)
            {
                onImprovePurchased(currentPlaceId, index, purchasedLevel);
            }
            uiItem.Refresh();
            return;
        }
        PurchaseImprovement_Legacy(index, uiItem);
    }

    public bool PurchaseImprovementUpgrade(int index, ImprovementItemUI item)
    {
        if (UseWorldMotorForPlace(currentPlaceId))
        {
            int maxBuyCount = 1;
            if (pELiNhaLLitSIJa.Instance != null)
                maxBuyCount = Mathf.Max(1, pELiNhaLLitSIJa.Instance.getMultiplyCount());

            int purchasedLevel;
            bool worldDoubleSpeed;
            bool success = TryPurchaseWorldImprovement(currentPlaceId, index, maxBuyCount, out purchasedLevel, out worldDoubleSpeed);
            if (success && onImprovePurchased != null)
            {
                onImprovePurchased(currentPlaceId, index, purchasedLevel);
            }

            item.ItemUpgraded();
            return worldDoubleSpeed;
        }

        return PurchaseImprovementUpgrade_Legacy(index, item);
    }

    public double FillIdleEarnings(double totalSeconds)
    {
        if (totalSeconds <= 0d)
            return 0d;

        if (useWorldDefinitionMotor)
        {
            double worldTotalEarning = TickWorldDefinitionMotor(totalSeconds, syncUIData: true);
            if (placeDictionary != null && World != null)
            {
                foreach (var kvp in placeDictionary)
                {
                    if (kvp.Key < World.Regions.Count || kvp.Value == null)
                        continue;

                    double profit = kvp.Value.GetProfitPerSec();
                    double potentialEarning = profit * totalSeconds;
                    double acceptedEarning = kvp.Value.AddMoneyToVault(potentialEarning);
                    worldTotalEarning += acceptedEarning;
                }
            }
            return worldTotalEarning;
        }
        Debug.LogError("Not implemented!!!!!");
        return 0;

        // double totalEarning = 0;
        // if (placeDictionary != null)
        // {
        //     foreach(var kvp in placeDictionary)
        //     {
        //         if (kvp.Value != null)
        //         {
        //             double profit = kvp.Value.GetProfitPerSec();
        //             double potentialEarning = profit * totalSeconds;
        //             double acceptedEarning = kvp.Value.AddMoneyToVault(potentialEarning);
        //             totalEarning += acceptedEarning;
        //             #if SOFTCEN_DEBUG
        //             Debug.Log($"***** FillIdleEarnings {kvp.Value.name}, potential: {potentialEarning}, accepted: {acceptedEarning}");
        //             #endif
        //         }
        //     }
        // }
        // #if SOFTCEN_DEBUG
        // Debug.Log($"***** FillIdleEarnings totalEarning: {totalEarning}");
        // #endif
        // return totalEarning;
    }

    public double GetProfitPerSec()
    {
        if (useWorldDefinitionMotor && World != null)
        {
            double worldProfit = 0d;
            double multiplier = GetProfitMultiplier();
            // int placeCount = placeDictionary != null ? placeDictionary.Count : 0;
            int regionCount = World.Regions.Count;
            for (int regionIndex = 0; regionIndex < regionCount; regionIndex++)
            {
                RegionState region = World.Regions[regionIndex];
                if (region == null || !region.IsUnlocked)
                    continue;

                for (int improvementIndex = 0; improvementIndex < region.Improvements.Count; improvementIndex++)
                {
                    ImprovementState improvement = region.Improvements[improvementIndex];
                    if (improvement == null || !improvement.IsUnlocked)
                        continue;

                    double duration = improvement.GetCurrentDuration();
                    if (duration <= 0d)
                        continue;

                    worldProfit += (improvement.GetCurrentProfit() * multiplier) / duration;
                }
            }

            // if (placeDictionary != null)
            // {
            //     foreach (var kvp in placeDictionary)
            //     {
            //         if (kvp.Key < World.Regions.Count || kvp.Value == null)
            //             continue;
            //         worldProfit += kvp.Value.GetProfitPerSec();
            //     }
            // }
            return worldProfit;
        }

        // double profit = 0;
        // if (placeDictionary != null)
        // {
        //     foreach(var kvp in placeDictionary)
        //     {
        //         if (kvp.Value != null)
        //         {
        //             profit += kvp.Value.GetProfitPerSec();
        //         }
        //     }
        // }
        // return profit;
        return 0;
    }

    public string GetCurrentRegionDisplayName()
    {
        if (currentRegionState == null || currentRegionState.Definition == null) return "-";
        return currentRegionState.Definition.displayName;
    }

    private void UpdateRegionTitle()
    {
        // if (txtNyKYInenRAta == null) return;
        // if (currentRegionState == null || currentRegionState.Definition == null)
        // {
        //     txtNyKYInenRAta.SetText("-");
        // }
        // else
        // {
        //     txtNyKYInenRAta.SetText(currentRegionState.Definition.displayName);
        // }
    }

    // public string AnNnaPaIkaNNImI(int index)
    // {
    //     if (placeDictionary.ContainsKey(currentPlaceId))
    //     {
    //         return placeDictionary[currentPlaceId].PlaceName;
    //     }
    //     // if (index < places.Count)
    //     //     return places[index].PlaceName;
    //     return "";
    // }


    public void AnnAPaikkaTIEdot(int index, out string name, out int currentVault, out int maxVault, out double vaultSize)
    {
        RegionState region = World.TryToGetRegion(index);
        if (region != null && region.Definition != null)
        {
            name = region.Definition != null ? region.Definition.displayName : "";
            currentVault = pELiNhaLLitSIJa.Instance.playerData.GetVaultOwned(index);
            maxVault = region.Definition.vaultSizes != null ? region.Definition.vaultSizes.Length : 0;
            if (maxVault <= 0) vaultSize = 0d;
            else if (currentVault < maxVault) vaultSize = region.Definition.vaultSizes[currentVault];
            else vaultSize = region.Definition.vaultSizes[maxVault-1];
        }
        // if (placeDictionary.ContainsKey(index))
        // {
        //     PlaceItem place = placeDictionary[index];
        //     name = place.PlaceName;
        //     currentVault = pELiNhaLLitSIJa.Instance.playerData.aNNaPaiKKaKassaKAAppi(index);
        //     maxVault = (place.kassaKaappiKoot != null) ? place.kassaKaappiKoot.Length : 0;

        //     if (maxVault <= 0)
        //         vaultSize = 0d;
        //     else if (currentVault < maxVault)
        //         vaultSize = place.kassaKaappiKoot[currentVault];
        //     else
        //         vaultSize = place.kassaKaappiKoot[maxVault - 1];
        // }
        else
        {
            name = "";
            currentVault = 0;
            maxVault = 0;
            vaultSize = 0;
        }
    }

    public double CollectAllVaults()
    {
        double totalCollected = 0d;
        if (World == null) return totalCollected;
        totalCollected += World.CollectAllVaults();
        if (totalCollected > 0d && pELiNhaLLitSIJa.Instance != null) currentRegionState.AddMoney(totalCollected);
        // pELiNhaLLitSIJa.Instance.ChangeMoney(totalCollected);

        return totalCollected;

        // if (placeDictionary == null)
        //     return totalCollected;

        // foreach (var kvp in placeDictionary)
        // {
        //     PlaceItem place = kvp.Value;
        //     if (place == null)
        //         continue;

        //     double amount = place.Money;
        //     if (amount <= 0d)
        //         continue;

        //     totalCollected += amount;
        //     place.DecreaseMoney(amount);
        // }

        // if (totalCollected > 0d && pELiNhaLLitSIJa.Instance != null)
        //     pELiNhaLLitSIJa.Instance.ChangeMoney(totalCollected);

        // return totalCollected;
    }

    public void SeurAAvaPAiKKA()
    {
        paIVitANapiT_Legacy(1);
    }

    public void eDElLINenPAiKKA()
    {
        paIVitANapiT_Legacy(-1);
    }

    private IEnumerator LoadLevel()
    {
        RegionState region = World.TryToGetRegion(currentPlaceId);
        if (region != null && region.Definition != null)
        {
            ResourceRequest request = Resources.LoadAsync(region.Definition.resourceName, typeof(GameObject));
            yield return request;
            GameObject goLvl = Instantiate(request.asset as GameObject) as GameObject;
            if (goLvl != null)
            {
                if (goNyKyiNENPaikka != null)
                    Destroy(goNyKyiNENPaikka);
                goNyKyiNENPaikka = goLvl;
                paikkaObjekti = goNyKyiNENPaikka.GetComponent<Place>();
                #if SOFTCEN_DEBUG
                Debug.Assert(paikkaObjekti != null, "paikkaObjekti Null");
                #endif
                goNyKyiNENPaikka.SetActive(true);
                // Aseta effect pools??
                // TODO: Effectit resussiin
                // for (int i = 0; i < paikkaObjekti.placeObjects.Length; i++) {
                //     if (region.Improvements != null)
                //     {
                //         // if (i < item.improvements.Count && item.improvements[i] != null) {
                //         //     paikkaObjekti.placeObjects [i]._effectPool = item.improvements[i]._effectPool;
                //         // }
                //     }
                // }
            }
            SetTila(paikkaTila.VALMIS);
        }
    }

    private bool UseWorldMotorForPlace(int placeId)
    {
        if (!useWorldDefinitionMotor || World == null) // || placeDictionary == null)
            return false;

        if (placeId < 0 || placeId >= World.Regions.Count)
            return false;

        // if (!placeDictionary.TryGetValue(placeId, out PlaceItem place) || place == null || place.improvements == null)
        //     return false;

        RegionState region = World.Regions[placeId];
        if (region == null || region.Improvements == null)
            return false;

        return region.Improvements.Count > 0;
    }

    private void InitializeWorldDefinitionMotor()
    {
        if (!useWorldDefinitionMotor || World == null)
            return;

        World.UnlockAllRegions();
        SyncWorldLevelsFromPlayerData();
        DisableLegacyImprovementUpdates();
        SyncAllPlacesImprovementData();
    }

    private IEnumerator InitializeWorldDefinitionMotorDelayed()
    {
        yield return null;
        InitializeWorldDefinitionMotor();
    }

    private void DisableLegacyImprovementUpdates()
    {
        if (placeDictionary == null)
            return;

        foreach (var kvp in placeDictionary)
        {
            if (!UseWorldMotorForPlace(kvp.Key))
                continue;

            PlaceItem place = kvp.Value;
            if (place == null || place.improvements == null)
                continue;

            int count = Mathf.Min(place.improvements.Count, World.Regions[kvp.Key].Improvements.Count);
            for (int i = 0; i < count; i++)
            {
                ImprovementItem improvement = place.improvements[i];
                if (improvement != null)
                    improvement.enabled = false;
            }
        }
    }

    private void SyncWorldLevelsFromPlayerData()
    {
        if (World == null)
            return;

        pELiNhaLLitSIJa gm = pELiNhaLLitSIJa.Instance;
        if (gm == null || gm.playerData == null)
            return;

        for (int i=0; i < World.Regions.Count; i++)
        {
            RegionState region = World.Regions[i];
            if (region == null || region.Improvements == null || region.Definition == null) continue;
            bool freshRegion = true;
            for (int j=0; j < region.Improvements.Count; j++)
            {
                if (region.Improvements[j].Definition == null) continue;
                int savedLevel = gm.playerData.GetPlaceItemLevel(region.Definition.id, region.Improvements[j].Definition.id);
                if (savedLevel > 0) freshRegion = false;
                World.SetImprovementLevel(region.Definition.id, region.Improvements[j].Definition.id, savedLevel, resetProgress: true);
            }
            if (freshRegion)
            {
                region.AddMoney(region.Definition.firstImprovementPrice);
            }
        }

        // foreach (var kvp in placeDictionary)
        // {
        //     int placeId = kvp.Key;
        //     PlaceItem place = kvp.Value;
        //     if (place == null || place.improvements == null)
        //         continue;

        //     if (placeId < 0 || placeId >= World.Regions.Count)
        //         continue;

        //     RegionState region = World.Regions[placeId];
        //     int count = Mathf.Min(place.improvements.Count, region.Improvements.Count);
        //     for (int i = 0; i < count; i++)
        //     {
        //         ImprovementItem improvement = place.improvements[i];
        //         if (improvement == null)
        //             continue;

        //         int savedLevel = gm.playerData.GetPlaceItemLevel(placeId, improvement.id);
        //         World.SetImprovementLevel(placeId, i, savedLevel, resetProgress: true);
        //     }
        // }
    }

    private double TickWorldDefinitionMotor(double deltaSeconds, bool syncUIData)
    {
        if (!useWorldDefinitionMotor || World == null || deltaSeconds <= 0d)
            return 0d;

        double totalEarning = 0d;
        //int placeCount = placeDictionary != null ? placeDictionary.Count : 0;
        int regionCount = World.Regions.Count;
        double multiplier = GetProfitMultiplier();

        for (int regionIndex = 0; regionIndex < regionCount; regionIndex++)
        {
            RegionState region = World.Regions[regionIndex];
            if (region == null || !region.IsUnlocked)
                continue;

            double earnedByRegion = 0d;
            for (int improvementIndex = 0; improvementIndex < region.Improvements.Count; improvementIndex++)
            {
                ImprovementState improvement = region.Improvements[improvementIndex];
                if (improvement == null)
                    continue;

                double earned = improvement.Tick(deltaSeconds);
                if (earned > 0d)
                    earnedByRegion += earned * multiplier;
            }

            if (earnedByRegion <= 0d)
                continue;

            Debug.Log($"AddMoneyToVault earnedByRegion: {earnedByRegion}");
            region.AddMoneyToVault(earnedByRegion);

            // if (placeDictionary.TryGetValue(regionIndex, out PlaceItem place) && place != null)
            // {
            //     double accepted = place.AddMoneyToVault(earnedByRegion);
            //     totalEarning += accepted;
            // }
        }

        // if (syncUIData)
        // {
        //     SyncAllPlacesImprovementData();
        //     PlaceItem_OnCollectedMoneyChanged_Legacy();
        // }
        // else
        // {
        //     SyncPlaceImprovementData(currentPlaceId);
        // }

        return totalEarning;
    }

    private bool TryPurchaseWorldImprovement(int placeId, int improvementIndex, int maxCount, out int purchasedLevel, out bool doubleSpeed)
    {
        purchasedLevel = 0;
        doubleSpeed = false;

        pELiNhaLLitSIJa gm = pELiNhaLLitSIJa.Instance;
        if (currentRegionState == null || gm == null) return false;
        // if (!UseWorldMotorForPlace(placeId) || World == null || pELiNhaLLitSIJa.Instance == null || pELiNhaLLitSIJa.Instance.playerData == null)
        //     return false;

        RegionState region = World.Regions[placeId];
        if (region == null || region.Improvements == null || improvementIndex < 0 || improvementIndex >= region.Improvements.Count || region.Improvements[improvementIndex].Definition == null)
            return false;

        // if (placeDictionary == null || !placeDictionary.TryGetValue(placeId, out PlaceItem place) || place == null || place.improvements == null || improvementIndex >= place.improvements.Count)
        //     return false;

        // ImprovementItem placeImprovement = place.improvements[improvementIndex];
        // if (placeImprovement == null)
        //     return false;

        double regionMoney = currentRegionState.GetMoney();
        double wallet = regionMoney; //gm.money;
        int oldLevel = region.Improvements[improvementIndex].Level;
        int purchases = 0;
        int maxPurchases = Mathf.Max(1, maxCount);

        for (int i = 0; i < maxPurchases; i++)
        {
            if (!region.CanBuyImprovement(improvementIndex, wallet))
                break;

            bool success = region.TryBuyImprovement(improvementIndex, ref wallet);
            if (!success)
                break;

            purchases++;

            int newLevel = region.Improvements[improvementIndex].Level;
            if (IsDoubleSpeedMilestoneCrossed(oldLevel, newLevel))
                doubleSpeed = true;

            oldLevel = newLevel;
        }

        if (purchases <= 0)
            return false;

        double spent = regionMoney - wallet;
        if (spent > 0d)
        {
            currentRegionState.DecreaseMoney(spent);
            //gm.ChangeMoney(-spent);
        }

        for (int i = 0; i < purchases; i++)
        {
            bool callEvent = i == purchases - 1;
            int id = region.Improvements[improvementIndex].Definition.id;
            gm.PurchaseUpgradeItem(placeId, id, callEvent);
        }

        //SyncPlaceImprovementData(placeId);
        purchasedLevel = region.Improvements[improvementIndex].Level;
        return true;
    }

    public bool HasGigStarted()
    {
        if (currentRegionState == null) return false;
        return currentRegionState.HasGigStarted();
    }
    private bool IsDoubleSpeedMilestoneCrossed(int oldLevel, int newLevel)
    {
        for (int i = 0; i < starMilestones.Length; i++)
        {
            int milestone = starMilestones[i];
            if (oldLevel < milestone && newLevel >= milestone)
                return true;
        }
        return false;
    }

    private void SyncAllPlacesImprovementData()
    {
        if (placeDictionary == null)
            return;

        // foreach (var kvp in placeDictionary)
        // {
        //     SyncPlaceImprovementData(kvp.Key);
        // }
    }

    // private void SyncPlaceImprovementData(int placeId)
    // {
    //     if (!UseWorldMotorForPlace(placeId) || World == null || placeDictionary == null)
    //         return;

    //     if (!placeDictionary.TryGetValue(placeId, out PlaceItem place) || place == null || place.improvements == null)
    //         return;

    //     if (placeId < 0 || placeId >= World.Regions.Count)
    //         return;

    //     RegionState region = World.Regions[placeId];
    //     if (region == null)
    //         return;

    //     int count = Mathf.Min(place.improvements.Count, region.Improvements.Count);
    //     for (int i = 0; i < count; i++)
    //     {
    //         ImprovementItem item = place.improvements[i];
    //         ImprovementState state = region.Improvements[i];
    //         if (item == null || state == null)
    //             continue;

    //         ApplyWorldImprovementToItem(item, state, place);
    //     }
    // }

    private void ApplyWorldImprovementToItem(ImprovementItem item, ImprovementState state, PlaceItem place)
    {
        int level = state.Level;
        double duration = state.GetCurrentDuration();
        if (duration <= 0d)
            duration = 1d;

        item.placeItem = place;
        item.owned = level > 0;
        item.level = level;
        item.starsCount = state.Stars;
        item.levelProgress = GetLevelProgress(level);

        double nextCost = state.GetNextUpgradeCost();
        item.currentPrice = nextCost;
        NumToStr.GetNumStr(nextCost, out item.strCurrentPriceSuffix, out item.strCurrentPrice);

        double profit = state.GetCurrentProfit() * GetProfitMultiplier();
        item.currentProfit = profit;
        item.strCurrentProfit = NumToStr.GetNumStr(profit);

        double progressSeconds = state.ProgressSeconds;
        if (progressSeconds < 0d)
            progressSeconds = 0d;
        if (progressSeconds > duration)
            progressSeconds = duration;

        item._currentDuration = (float)duration;
        item._timer = item.owned ? Mathf.Max(0f, (float)(duration - progressSeconds)) : (float)duration;
        item.progress = item.owned && duration > 0d ? Mathf.Clamp01((float)(progressSeconds / duration)) : 0f;
    }

    private double GetProfitMultiplier()
    {
        pELiNhaLLitSIJa gm = pELiNhaLLitSIJa.Instance;
        if (gm == null || gm.Kerroin == null)
            return 1d;

        double kerroin = gm.Kerroin.KerroinArvo;
        if (kerroin <= 0d)
            return 1d;

        return kerroin;
    }

    private static float GetLevelProgress(int level)
    {
        if (level <= 0 || level == 1 || level == 25 || level == 50 || level == 100 || level == 150 || level == 200)
            return 0f;

        if (level < 25)
            return (float)level / 25f;
        if (level < 50)
            return (float)(level - 25) / 25f;
        if (level < 100)
            return (float)(level - 50) / 50f;
        if (level < 150)
            return (float)(level - 100) / 50f;
        if (level < 200)
            return (float)(level - 150) / 50f;

        return 0f;
    }

    public bool TryGetWorldProfitForItem(ImprovementItem item, out double profit)
    {
        profit = 0d;
        if (!TryResolveWorldImprovement(item, out RegionState _, out int _, out ImprovementState state))
            return false;

        profit = state.GetCurrentProfit() * GetProfitMultiplier();
        return true;
    }

    public bool TryGetWorldNextUpgradeCost(ImprovementItem item, out double cost)
    {
        cost = 0d;
        if (!TryResolveWorldImprovement(item, out RegionState _, out int _, out ImprovementState state))
            return false;

        cost = state.GetNextUpgradeCost();
        return true;
    }

    public bool CanBuyWorldImprovement(ImprovementItem item, int maxCount, out double totalCost, out int buyCount)
    {
        totalCost = 0d;
        buyCount = 0;
        if (!TryResolveWorldImprovement(item, out RegionState region, out int improvementIndex, out ImprovementState state))
            return false;

        pELiNhaLLitSIJa gm = pELiNhaLLitSIJa.Instance;
        if (gm == null)
            return false;

        if (!TryGetWorldUpgradeCost(item, maxCount, out _, out _))
            return false;

        double wallet = currentRegionState.GetMoney(); // gm.money;
        int simulatedLevel = state.Level;
        int count = Mathf.Max(1, maxCount);

        for (int i = 0; i < count; i++)
        {
            if (!region.IsUnlocked)
                break;

            if (simulatedLevel >= ImprovementDefinition.MaxLevel)
                break;

            if (region.Definition.requirePreviousImprovementLevel25ForPurchase && improvementIndex > 0)
            {
                int previousLevel = region.Improvements[improvementIndex - 1].Level;
                if (previousLevel < 25)
                    break;
            }

            double nextCost = state.Definition.GetUpgradeCost(simulatedLevel);
            if (wallet < nextCost)
                break;

            wallet -= nextCost;
            totalCost += nextCost;
            simulatedLevel++;
            buyCount++;
        }

        return buyCount > 0;
    }

    public bool TryGetWorldUpgradeCost(ImprovementItem item, int maxCount, out double totalCost, out int buyCount)
    {
        totalCost = 0d;
        buyCount = 0;
        if (!TryResolveWorldImprovement(item, out RegionState region, out int improvementIndex, out ImprovementState state))
            return false;

        int simulatedLevel = state.Level;
        int count = Mathf.Max(1, maxCount);
        for (int i = 0; i < count; i++)
        {
            if (!region.IsUnlocked)
                break;

            if (simulatedLevel >= ImprovementDefinition.MaxLevel)
                break;

            if (region.Definition.requirePreviousImprovementLevel25ForPurchase && improvementIndex > 0)
            {
                int previousLevel = region.Improvements[improvementIndex - 1].Level;
                if (previousLevel < 25)
                    break;
            }

            totalCost += state.Definition.GetUpgradeCost(simulatedLevel);
            simulatedLevel++;
            buyCount++;
        }

        return buyCount > 0;
    }

    private bool TryResolveWorldImprovement(ImprovementItem item, out RegionState region, out int improvementIndex, out ImprovementState state)
    {
        region = null;
        state = null;
        improvementIndex = -1;

        if (!useWorldDefinitionMotor || World == null || item == null)
            return false;

        PlaceItem place = item.placeItem;
        if (place == null || !UseWorldMotorForPlace(place.id))
            return false;

        if (place.improvements == null)
            return false;

        improvementIndex = place.improvements.IndexOf(item);
        if (improvementIndex < 0)
            return false;

        region = World.Regions[place.id];
        if (region == null || improvementIndex >= region.Improvements.Count)
            return false;

        state = region.Improvements[improvementIndex];
        return state != null;
    }

    private void SetTila(paikkaTila newTila)
    {
        #if SOFTCEN_DEBUG
        Debug.Log($"SetTila old: {tila}, new: {newTila}");
        #endif
        tila = newTila;
    }

    // public bool VoikoKassakaapinAvata()
    // {
    //     if(places == null || places.Count <= 0) return false;
    //     for (int i=0; i < places.Count; i++)
    //     {
    //         if (places[i] != null && places[i].id == 0)
    //         {
    //             if (places[i].improvements != null && places[i].improvements.Count >= 2)
    //             {
    //                 return places[i].improvements[1].level > 5;
    //             }
    //             break;
    //         }
    //     }
    //     #if SOFTCEN_DEBUG
    //     Debug.LogError("Cannot find place id 0", gameObject);
    //     #endif
    //     return false;
    // }

    public bool TarvitaankoOpastusta()
    {
        return IsImprovementsProgressReached(improvementProgress.OPASTUS);
    }

    public bool IsImprovementsProgressReached(improvementProgress step)
    {
        if (World == null) return false;
        int firstImprovement = World.GetRegionImprovementLevel(0,0);
        int secondImprovement = World.GetRegionImprovementLevel(0,1);
        int thirdImprovement = World.GetRegionImprovementLevel(0,2);

        if (step == improvementProgress.OPASTUS && firstImprovement <= 5) return true;
        if (step == improvementProgress.STEP1 && firstImprovement > 5) return true;
        if (step == improvementProgress.STEP2 && secondImprovement > 5) return true;
        if (step == improvementProgress.STEP3 && thirdImprovement > 5) return true;

        // if (placeDictionary != null && placeDictionary.ContainsKey(0))
        // {
        //     var cplace = placeDictionary[0];
        //     if (cplace.improvements == null) return false;

        //     if (step == improvementProgress.OPASTUS)
        //     {
        //         if (cplace.improvements.Count >= 1) return cplace.improvements[0].level <= 5;
        //     }
        //     if (step == improvementProgress.STEP1)
        //     {
        //         if (cplace.improvements.Count >= 1) return cplace.improvements[0].level > 5;
        //     }
        //     if (step == improvementProgress.STEP2)
        //     {
        //         if (cplace.improvements.Count >= 2) return cplace.improvements[1].level > 5;
        //     }
        //     if (step == improvementProgress.STEP3)
        //     {
        //         if (cplace.improvements.Count >= 3) return cplace.improvements[2].level > 5;
        //     }
        // }

        return false;
    }

    public bool HasAnyProgress()
    {
        if (World == null) return false;
        int firstImprovement = World.GetRegionImprovementLevel(0,0);
        if (firstImprovement > 0) return true;

        // if (placeDictionary != null && placeDictionary.ContainsKey(0) && placeDictionary[0].improvements != null && placeDictionary[0].improvements.Count > 0)
        // {
        //     return placeDictionary[0].improvements[0].owned;
        // }
        return false;
    }

    public int GetPlacesCount()
    {
        if (World == null) return 0;
        return World.Regions.Count;

        // if (placeDictionary == null) return 0;
        // return placeDictionary.Count;
    }

    // Legacy functions:
    private void OnGameLoadedLegacy()
    {
        placeDictionary = new Dictionary<int, PlaceItem>();
		for (int i=0; i < transform.childCount; i++)
		{
			PlaceItem item = transform.GetChild(i).GetComponent<PlaceItem>();
			if (item != null)
			{
                if (!placeDictionary.ContainsKey(item.id))
                {
                    placeDictionary.Add(item.id, item);
                }
                else
                {
                    Debug.LogError("Same place Id already there", gameObject);
                }
				//places.Add(item);
			}
		}
        // Make id check
        for (int i=0; i < placeDictionary.Count; i++)
        {
            if (!placeDictionary.ContainsKey(i)) Debug.LogError("Place id mismatch!!! FIX THIS", gameObject);
        }
        //paivitaRata();

    }

    private void paivitaRata_Legacy()
    {
        currentPlace = getCurrentPlace_Legacy();
        PlaceItem_OnCollectedMoneyChanged_Legacy();
        PaiVITaRATaTieDOT_Legacy();
        if (currentPlace != null) {
            Vector3 pos = scrollView.content.position;
            pos.y = currentPlace.posY;
            //pos.y = places[currentPlaceId].posY;
            scrollView.content.position = pos;
            scrollView.Setup(10);
        }
        paIVitANapiT_Legacy(0);
        // Setup campath
        // camMoveAlongPath.path = paikkaObjekti.camPath;
        // camMoveAlongPath.speed = paikkaObjekti.camSpeed;
        // camMoveAlongPath.distance = 0f;
        // camMoveAlongPath.AddMovingObject(paikkaObjekti.keyFrameList, camTransform);
        //camMoveAlongPath.Sync();

    }

    private void PlaceItem_OnCollectedMoneyChanged_Legacy()
    {
        // if (currentRegionState != null)
        // {
        //     animatedNumberText.SetValue(currentRegionState.vaultMoney);
        //     return;
        // }
        // // Legacy:
        // if (currentPlace != null)
        // {
        //     animatedNumberText.SetValue(currentPlace.Money);
        // } else
        // {
        //     animatedNumberText.SetValue(0);
        // }

    }

    public void PaiVITaRATaTieDOT_Legacy()
    {
        // if (placeDictionary.ContainsKey(currentPlaceId))
        // {
        //     txtNyKYInenRAta.SetText(placeDictionary[currentPlaceId].PlaceName);
        // }
        // else
        // {
        //     txtNyKYInenRAta.SetText("-");
        // }
        // // if (currentPlaceId < places.Count)
        // // {
        // //     txtNyKYInenRAta.SetText(places[currentPlaceId].PlaceName);
        // // }
    }

    private void paIVitANapiT_Legacy(int change)
    {
        bool changed = false;
        int origIndex = currentPlaceId;
        if (change > 0)
        {
            if (currentPlaceId < (placeDictionary.Count-1))
            {
                currentPlaceId++;
                changed = true;
            }
        } else if (change < 0) {
            if (currentPlaceId > 0)
            {
                currentPlaceId--;
                changed = true;
            }
        }
        currentPlaceId = Mathf.Min(currentPlaceId, placeDictionary.Count - 1);
        currentPlaceId = Mathf.Max(currentPlaceId, 0);

        if (currentPlaceId == placeDictionary.Count-1)
        {
            btnNext.interactable = false;
        } else
        {
            btnNext.interactable = true;
        }
        if (currentPlaceId == 0)
        {
            btnPrev.interactable = false;
        } else
        {
            btnPrev.interactable = true;
        }

        if (changed)
        {
            if (placeDictionary.ContainsKey(origIndex)) placeDictionary[origIndex].posY = scrollView.content.position.y;
            //places[origIndex].posY = scrollView.content.position.y;
            if (change > 0)
                vaihtoPaneeli.seuraava();
            else
                vaihtoPaneeli.edellinen();

            vaihtoPaneeli.gameObject.SetActive(true);
            //tila = paikkaTila.ALOITA;
        }
    }

    private IEnumerator LoadLevel_Legacy()
    {
        if (placeDictionary.TryGetValue(currentPlaceId, out PlaceItem item))
        {
            ResourceRequest request = Resources.LoadAsync(item.reSURssINImi, typeof(GameObject));

            yield return request;
            //Debug.Log("Finishing LoadLevel");
            GameObject goLvl = Instantiate(request.asset as GameObject) as GameObject;
            if (goLvl != null)
            {
                if (goNyKyiNENPaikka != null)
                    Destroy(goNyKyiNENPaikka);
                goNyKyiNENPaikka = goLvl;
                paikkaObjekti = goNyKyiNENPaikka.GetComponent<Place>();
                #if SOFTCEN_DEBUG
                Debug.Assert(paikkaObjekti != null, "paikkaObjekti Null");
                #endif
                goNyKyiNENPaikka.SetActive(true);
                // Aseta effect pools??
                for (int i = 0; i < paikkaObjekti.placeObjects.Length; i++) {
                    if (item.improvements != null)
                    {
                        if (i < item.improvements.Count && item.improvements[i] != null) {
                            paikkaObjekti.placeObjects [i]._effectPool = item.improvements[i]._effectPool;
                        }
                        // if (i < places [currentPlaceId].improvements.Count) {
                        //     paikkaObjekti.placeObjects [i]._effectPool = places [currentPlaceId].improvements [i]._effectPool;
                        // }
                    }
                }
            }
            SetTila(paikkaTila.VALMIS);

        }
    }

    // private void MoneyCollectedPanel_OnCollectedMoneyPressed_Legacy()
    // {
    //     double amount = currentPlace.Money;
    //     if (amount > 0)
    //     {
    //         SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_PICKUPCOINS);
    //         GameObject go = poolCoinCollect.GetPooledObject();
    //         go.transform.position = trCoinCollect.position;
    //         go.SetActive(true);
    //         //currentPlace.ChangeMoney(-1d * amount);
    //         pELiNhaLLitSIJa.Instance.ChangeMoney(amount);
    //         currentPlace.DecreaseMoney(amount);
    //         //Debug.Log("amount: " + amount + ", collected money: " + currentPlace.money);

    //     }
    // }

    public ImprovementItem GetImprovementItem_Legacy(int index)
    {
        if (currentPlace == null || currentPlace.gameObject == null)
        {
            #if SOFTCEN_DEBUG
            Debug.LogWarning($"paIkKaHaLlItSiJa GetImprovementItem index: index: {index}, currentPlace is null");
            #endif
            return null;
        }
        #if SOFTCEN_DEBUG
        Debug.Log($"paIkKaHaLlItSiJa GetImprovementItem index: currentPlace: {currentPlace.gameObject.name}, index: {index}", currentPlace.gameObject);
        #endif
        // if (UseWorldMotorForPlace(currentPlaceId))
        // {
        //     SyncPlaceImprovementData(currentPlaceId);
        // }

        if (currentPlace != null && index >= 0 && index < currentPlace.improvements.Count)
            return currentPlace.improvements[index];

        #if SOFTCEN_DEBUG
        Debug.LogWarning("null improvement");
        #endif
        return null;
    }

    private void PurchaseImprovement_Legacy(int index, ImprovementItemUI item)
    {
        if (currentPlace == null)
            return;

        if (index < currentPlace.improvements.Count)
        {
            if (currentPlace.improvements[index].Purchase())
            {
                if (onImprovePurchased != null)
                {
                    onImprovePurchased(currentPlaceId, index, currentPlace.improvements[index].level);
                }
            }
        }
        item.Refresh();

    }

    private bool PurchaseImprovementUpgrade_Legacy(int index, ImprovementItemUI item)
    {
        if (currentPlace == null)
            return false;

        bool doubleSpeed = false;
        if (index < currentPlace.improvements.Count)
        {
            if (currentPlace.improvements[index].Upgrade())
            {
				doubleSpeed = currentPlace.improvements[index].CheckDoubleSpeed ();
				if (onImprovePurchased != null) {
					onImprovePurchased (currentPlaceId, index, currentPlace.improvements [index].level);
				}
            }
            if (doubleSpeed)
            {
                item.ActivateStar(currentPlace.improvements[index].GetStarsCount());
            }
        }
        item.ItemUpgraded();
		return doubleSpeed;
    }

    private PlaceItem getCurrentPlace_Legacy()
    {
        if (placeDictionary.TryGetValue(currentPlaceId, out PlaceItem item))
        {
            return item;
        }
        // for (int i=0; i < places.Count; i++)
        // {
        //     if (places[i].id == currentPlaceId)
        //     {
        //         return places[i];
        //     }
        // }
        return null;
    }

}
