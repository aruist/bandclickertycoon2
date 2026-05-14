using UnityEngine;
using UnityEngine.UI;
using System.Text;
using OneP.InfinityScrollView;
using TMPro;
using System;

public class ImprovementItemUI : MonoBehaviour {
    [SerializeField] private CanvasGroup canvasGroup;
    public Image imgItemIcon;
    public GameObject goPressParticle;
    public GameObject goActivePanel;
    public Button btnPurchaseItem;
    public Button btnUpgradeItem;
    public TextMeshProUGUI txtTitle;
    public TextMeshProUGUI txtItemPrice;
    public TextMeshProUGUI txtItemLevel;
    public TextMeshProUGUI txtDuration;
    public TextMeshProUGUI txtCurrentProfit;
    public TextMeshProUGUI txtUpgradePrice;
    public Image[] imgStars;
    public ParticleSystem[] parStars;
    public Image imgLevelProgress;
    public Image imgImproveProgress;
	public GameObject goDoubleSpeed;

    [SerializeField] private ImprovementItem item;
    [SerializeField] private ImprovementState improvementState;

    private StringBuilder sbTime;
    public string strGF;

	public ParticleSystem psEffect;

    private int _currentLevel = 0;
    private int _starsCount = 0;

    public float deactiveAlpha = 0.3f;
	private double currentPrice;

    private bool bMoneyAction = false;
    private int updatedMin = -1;
    private int updatedSec = -1;
    private readonly char[] timeChars = { '0', '0', ':', '0', '0' };
    private int Index;


    void Awake()
    {
        sbTime = new StringBuilder(6,6);
        sbTime.Append("00:00\0");

    }
    void OnEnable()
    {
        //paIkKaHaLlItSiJa.OnRegionChanged += OnRegionChanged;
        PlayerData.OnMoneyChanged += GameManager_OnMoneyChanged;
        if (!bMoneyAction)
        {
            bMoneyAction = true;
            pELiNhaLLitSIJa.OnMultiplyChanged += GameManager_OnMultiplyChanged;
        }
        keRrOIn.OnkeRrOInMuuTTUnut += GameManager_OnMultiplerBonusChanged;
    }

    void OnDisable()
    {
        //paIkKaHaLlItSiJa.OnRegionChanged -= OnRegionChanged;
        if (bMoneyAction)
        {
            bMoneyAction = false;
            PlayerData.OnMoneyChanged -= GameManager_OnMoneyChanged;
        }
        pELiNhaLLitSIJa.OnMultiplyChanged -= GameManager_OnMultiplyChanged;
        keRrOIn.OnkeRrOInMuuTTUnut -= GameManager_OnMultiplerBonusChanged;
    }

    public void Bind(ImprovementState state, int stateindex)
    {
        Index = stateindex;
        improvementState = state;
        RefreshUI();
    }

    // private void OnRegionChanged()
    // {
    //     RefreshUI();
    // }

    void GameManager_OnMultiplyChanged ()
    {
        if (paIkKaHaLlItSiJa.Instance == null || improvementState == null || !improvementState.IsUnlocked) return;

		double price = getCurrentPrice();
		txtUpgradePrice.SetText(NumToStr.GetNumStr(price));
        double regionMoney = paIkKaHaLlItSiJa.Instance.GetCurrentRegionMoney();
        if (price > 0d && price <= regionMoney)
        {
            btnUpgradeItem.interactable = true;
        }
        else
        {
            btnUpgradeItem.interactable = false;
        }

    }

    private void KeRrOIn_OnkeRrOInMuuTTUnut()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("KeRrOIn_OnkeRrOInMuuTTUnut");
        #endif
    }

    private void GameManager_OnMultiplerBonusChanged()
    {
        if (improvementState != null)
        {
            double currentProfit = improvementState.GetCurrentProfit();
            txtCurrentProfit.SetText(NumToStr.GetNumStr(currentProfit));
            return;
        }
        if (item == null) return;
        item.UpdateProfit();
        txtCurrentProfit.SetText(item.strCurrentProfit);
    }

    public static string GarbageFreeString(StringBuilder sb)
    {
        string str = (string)sb.GetType().GetField(
            "_str",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance).GetValue(sb);

        //Optional: clear out the string
        //for (int i = 0; i &lt; sb.Capacity; i++) {
        //	sb.Append(" ");
        //}
        return str;
    }

	private double getCurrentPrice() {
        paIkKaHaLlItSiJa placesManager = paIkKaHaLlItSiJa.Instance;
        pELiNhaLLitSIJa gameManager = pELiNhaLLitSIJa.Instance;
        if (placesManager != null && gameManager != null && item != null && placesManager.UsesWorldDefinitionMotor)
        {
            int count = gameManager.getMultiplyCount();
            if (placesManager.TryGetWorldUpgradeCost(item, count, out double worldCost, out int buyCount))
            {
                return buyCount > 0 ? worldCost : 0d;
            }
            return 0d;
        }

        int legacyCount = pELiNhaLLitSIJa.Instance.getMultiplyCount();
		return legacyCount * currentPrice;
    }

    private void GameManager_OnMoneyChanged()
    {
        paIkKaHaLlItSiJa ph = paIkKaHaLlItSiJa.Instance;
        if (ph == null) return;

        double regionMoney = ph.GetCurrentRegionMoney();
        if (improvementState == null)
        {
            GameManager_OnMoneyChangedLegacy();
            return;
        }
        if (improvementState.IsUnlocked)
        {
            double price = getCurrentPrice ();

            if (price > 0d && price <= regionMoney)
            {
                btnUpgradeItem.interactable = true;
            }
            else
            {
                btnUpgradeItem.interactable = false;
            }
        }
        else
        {
            double purchasePrice = improvementState.GetNextUpgradeCost();
            // paIkKaHaLlItSiJa placesManager = paIkKaHaLlItSiJa.Instance;
            // if (placesManager != null && placesManager.UsesWorldDefinitionMotor)
            // {
            //     if (placesManager.TryGetWorldNextUpgradeCost(item, out double worldPurchasePrice))
            //         purchasePrice = worldPurchasePrice;
            //     canBuy = placesManager.CanBuyWorldImprovement(item, 1, out _, out _);
            // }

            if (purchasePrice <= regionMoney && purchasePrice > 0d)
            {
                btnPurchaseItem.interactable = true;
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
            else
            {
                btnPurchaseItem.interactable = false;
                canvasGroup.alpha = deactiveAlpha;
                canvasGroup.interactable = false;
            }
        }
    }

    public void UpdateUI() {

    }

    void Update()
    {
        if (improvementState != null)
        {
            imgImproveProgress.fillAmount = improvementState.GetProgress();
            UpdateTime(improvementState.GetTimeLeft());
            return;
        }
        if (item == null || !item.owned)
            return;

        imgImproveProgress.fillAmount = item.progress;
        UpdateTime(item._timer);
    }

    // public override void Reload(InfinityScrollView _infinity, int _index)
    // {
    //     base.Reload(_infinity, _index);
    //     //txtDuration.text = "00:00";
    //     UpdateItem();
    //     //UpdateTime();
    // }

    private void UpdateTime(float timeLeft)
    {
        //int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(timeLeft));
        int totalSeconds = timeLeft > 0f ? Mathf.CeilToInt(timeLeft) : 0;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds - minutes * 60;
        if (updatedMin == minutes && updatedSec == seconds)
            return;

        updatedMin = minutes;
        updatedSec = seconds;

        // Supports 00:00 - 99:59
        minutes = Mathf.Min(minutes, 99);
        timeChars[0] = (char)('0' + minutes / 10);
        timeChars[1] = (char)('0' + minutes % 10);
        timeChars[3] = (char)('0' + seconds / 10);
        timeChars[4] = (char)('0' + seconds % 10);
        txtDuration.SetCharArray(timeChars);
    }

    private void RefreshUI()
    {
        if (improvementState == null || improvementState.Definition == null) return;

        if (imgItemIcon != null) imgItemIcon.sprite = improvementState.Definition.sprite;

        txtTitle.SetText(improvementState.Definition.displayName);
        if (improvementState.IsUnlocked)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;

            goActivePanel.SetActive(true);
            btnPurchaseItem.gameObject.SetActive(false);
            _starsCount = improvementState.Stars;
            for (int i=0; i < imgStars.Length; i++)
            {
                Color col = imgStars[i].color;
                if (_starsCount > i)
                {
                    col.a = 1f;
                } else
                {
                    col.a = 0.2f;
                }
                imgStars[i].color = col;
            }
            _currentLevel = improvementState.Level;
            txtItemLevel.SetText(_currentLevel.ToString());
            imgLevelProgress.fillAmount = improvementState.GetLevelProgress();
            double currentProfit = improvementState.GetCurrentProfit();
            txtCurrentProfit.SetText(NumToStr.GetNumStr(currentProfit));
            currentPrice = improvementState.GetNextUpgradeCost();
            GameManager_OnMultiplyChanged ();
        }
        else
        {
            canvasGroup.alpha = deactiveAlpha;
            canvasGroup.interactable = false;

            goActivePanel.SetActive(false);
            btnPurchaseItem.gameObject.SetActive(true);
            double purchasePrice = improvementState.GetNextUpgradeCost();
            paIkKaHaLlItSiJa placesManager = paIkKaHaLlItSiJa.Instance;
            if (placesManager != null && placesManager.UsesWorldDefinitionMotor)
            {
                if (placesManager.TryGetWorldNextUpgradeCost(item, out double worldPurchasePrice))
                    purchasePrice = worldPurchasePrice;
            }

            if (txtItemPrice != null) txtItemPrice.SetText(NumToStr.GetNumStr(purchasePrice));
        }

        GameManager_OnMoneyChanged();
        checkUpgradeButton();
    }

    private void UpdateItem()
    {
        improvementState = paIkKaHaLlItSiJa.Instance.GetImprovementState(Index);
        #if SOFTCEN_DEBUG
        Debug.Log($"ImprovementItemUI - UpdateItem improvementState: {improvementState != null}", gameObject);
        #endif
        if (improvementState == null)
        {
            UpdateItemLegacy();
            return;
        }
        RefreshUI();
    }

    public void osTApARaNNuS()
    {
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_PURCHASE_UPGRADE);
        paIkKaHaLlItSiJa.Instance.PurchaseImprovement(Index, this);
    }

    private void checkUpgradeButton()
    {
        if (improvementState == null)
        {
            checkUpgradeButtonLegacy();
            return;
        }

        if (improvementState.Level >= GameConsts.Game.maxItemLevel)
        {
            btnUpgradeItem.gameObject.SetActive(false);
            if (bMoneyAction)
            {
                bMoneyAction = false;
                PlayerData.OnMoneyChanged -= GameManager_OnMoneyChanged;
            }
        }
        else
        {
            btnUpgradeItem.gameObject.SetActive(true);
        }
    }

    public void pAIvitAPAraNnuS()
    {
        psEffect.Play ();
        SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_KEYBOARD_CLICK);
        if (!goPressParticle.activeSelf)
            goPressParticle.SetActive(true);
        if (paIkKaHaLlItSiJa.Instance.PurchaseImprovementUpgrade (Index, this)) {
			goDoubleSpeed.SetActive (true);
            SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_DOUBLE_SPEED);
        }
        checkUpgradeButton();
    }

    public void ActivateStar(int starIndex)
    {
        if (starIndex >= 1 && starIndex <= parStars.Length)
        {
            SoundFXManager.PlayUIOneShot(SoundFXManager.DefaultSounds.UI_STARPOPUP);
            parStars[starIndex-1].Play();
        }
    }

    public void Refresh()
    {
        UpdateItem();
    }

    public void ItemUpgraded()
    {
        UpdateItem();
    }

    private void UpdateItemLegacy()
    {
        item = paIkKaHaLlItSiJa.Instance.GetImprovementItem_Legacy(Index);
        if (item == null)
        {
            return;
        }
        if (item != null)
        {
            #if SOFTCEN_DEBUG
            Debug.Log($"ImprovementItemUI - UpdateItem {gameObject.name} item name: {item.gameObject.name}", item.gameObject);
            #endif
            txtTitle.SetText(item.name);
            if (item.owned)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;

                goActivePanel.SetActive(true);
                btnPurchaseItem.gameObject.SetActive(false);
                _starsCount = item.GetStarsCount();
                for (int i=0; i < imgStars.Length; i++)
                {
                    Color col = imgStars[i].color;
                    if (_starsCount > i)
                    {
                        col.a = 1f;
                    } else
                    {
                        col.a = 0.2f;
                    }
                    imgStars[i].color = col;
                }
                _currentLevel = item.level;
                txtItemLevel.SetText(_currentLevel.ToString());
                imgLevelProgress.fillAmount = item.levelProgress;
                txtCurrentProfit.SetText(item.strCurrentProfit);
				currentPrice = item.currentPrice;
				GameManager_OnMultiplyChanged ();
            }
            else
            {
                canvasGroup.alpha = deactiveAlpha;
                canvasGroup.interactable = false;

                goActivePanel.SetActive(false);
                btnPurchaseItem.gameObject.SetActive(true);
                double purchasePrice = item.startingPrice;
                paIkKaHaLlItSiJa placesManager = paIkKaHaLlItSiJa.Instance;
                if (placesManager != null && placesManager.UsesWorldDefinitionMotor)
                {
                    if (placesManager.TryGetWorldNextUpgradeCost(item, out double worldPurchasePrice))
                        purchasePrice = worldPurchasePrice;
                }

                if (txtItemPrice != null) txtItemPrice.SetText(NumToStr.GetNumStr(purchasePrice));
            }
        }
        else
        {
            Debug.Log("ImprovementItemUI item null " + Index);
        }
        GameManager_OnMoneyChanged();
        checkUpgradeButton();
    }

    private void GameManager_OnMoneyChangedLegacy()
    {
        if (item == null || paIkKaHaLlItSiJa.Instance == null) return;
        double regionMoney = paIkKaHaLlItSiJa.Instance.GetCurrentRegionMoney();
        if (item.owned)
        {
            double price = getCurrentPrice ();
            if (price > 0d && price <= regionMoney)
            {
                btnUpgradeItem.interactable = true;
            }
            else
            {
                btnUpgradeItem.interactable = false;
            }
        }
        else
        {
            double purchasePrice = item.startingPrice;
            paIkKaHaLlItSiJa placesManager = paIkKaHaLlItSiJa.Instance;
            bool canBuy = true;
            if (placesManager != null && placesManager.UsesWorldDefinitionMotor)
            {
                if (placesManager.TryGetWorldNextUpgradeCost(item, out double worldPurchasePrice))
                    purchasePrice = worldPurchasePrice;
                canBuy = placesManager.CanBuyWorldImprovement(item, 1, out _, out _);
            }

            if (canBuy && purchasePrice <= regionMoney && purchasePrice > 0d)
            {
                btnPurchaseItem.interactable = true;
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
            }
            else
            {
                btnPurchaseItem.interactable = false;
                canvasGroup.alpha = deactiveAlpha;
                canvasGroup.interactable = false;
            }
        }
    }

    private void checkUpgradeButtonLegacy()
    {
        if (item == null)
            return;

        if (item.level >= GameConsts.Game.maxItemLevel)
        {
            btnUpgradeItem.gameObject.SetActive(false);
            if (bMoneyAction)
            {
                bMoneyAction = false;
                PlayerData.OnMoneyChanged -= GameManager_OnMoneyChanged;
            }
        }
        else
        {
            btnUpgradeItem.gameObject.SetActive(true);
        }
    }

    // void GameManager_OnMultiplyChangedLegacy ()
    // {
    //     if (item == null || !item.owned)
    //         return;

	// 	double price = getCurrentPrice();
	// 	txtUpgradePrice.SetText(NumToStr.GetNumStr(price));
    //     if (price > 0d && price <= pELiNhaLLitSIJa.Instance.money)
    //     {
    //         btnUpgradeItem.interactable = true;
    //     }
    //     else
    //     {
    //         btnUpgradeItem.interactable = false;
    //     }

    // }

}
