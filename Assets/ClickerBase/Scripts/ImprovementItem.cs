using UnityEngine;
using UnityEngine.UI;

public class ImprovementItem : MonoBehaviour {
	public int id;
    public PlaceItem placeItem;
    public string improvementName;
    public double startingPrice;
    public double startingProfit;
    public float startingTime;
    public bool owned = false;
    public AnimationCurve profitCurve;
    public AnimationCurve priceCurve;
    public int level = 0;


    public float _timer;
    public float _currentDuration;
    public double _currentMoney;

    public float progress;

    public string strCurrentProfit;

    public double currentProfit;

    public string strCurrentPrice;
    public string strCurrentPriceSuffix;
    public double currentPrice;
    public int starsCount;
    public float levelProgress;

    public bool analyzeItem = false;
    public float analyzeTotalTime;
    public float analyze25Time;
    public float analyze50Time;
    private pELiNhaLLitSIJa gm;
    private int _placeId;

	public ObjectPooler _effectPool;

    void Awake()
    {
		/*
		if (profitCurve != null) {
			string str = gameObject.name + "\n";
			for (int i = 0; i < 250; i++) {
				float value = profitCurve.Evaluate (Mathf.Lerp (0, 1, (float)i / 250f));
				str += ", " + value.ToString();
			}
			Debug.Log (str);
		}*/
    }
    void Start()
    {
        #if SOFTCEN_DEBUG
        //if (analyzeItem)
        //    AnalyzeItem(10);
        #endif
        _timer = SetTimer(level);
        _currentDuration = _timer;
        gm = pELiNhaLLitSIJa.Instance;
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
    }

    void Update()
    {
        if (!owned)
            return;

        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            progress = 1;
            _timer += _currentDuration;
            if (placeItem != null)
            {
                // #if SOFTCEN_DEBUG
                // Debug.Log($"ImprovementItem - Update Profit: {currentProfit}", gameObject);
                // #endif
                //gm.ChangeMoney(currentProfit);
                placeItem.ChangeMoney(currentProfit);
                // GameManager.Instance.ChangeMoney(currentProfit);
                // placeItem.ChangeMoney(currentProfit);

            }
        }
        else
        {
            progress = Mathf.Lerp(0, 1, (float)(_currentDuration - _timer) / _currentDuration);
        }
    }

    private float SetTimer(int lvl)
    {
        float duration;
		if (lvl < 25)
            duration = startingTime;
		else if (lvl < 50)
            duration = startingTime / 2f;
		else if (lvl < 100)
            duration = startingTime / 4f;
		else if (lvl < 150)
            duration = startingTime / 8f;
		else if (lvl < 200)
            duration = startingTime / 16f;
		else
            duration = startingTime / 32f;
		if (duration < 1f)
            duration = 1f;
		//_timer = _currentDuration;
        return duration;
    }
    public int GetStarsCount()
    {
        if (level < 25)
            return 0;
        if (level < 50)
            return 1;
        if (level < 100)
            return 2;
        if (level < 150)
            return 3;
        if (level < 200)
            return 4;

        return 5;
    }
    public float GetLevelProgress()
    {
        if (level == 1 || level == 25 || level == 50 || level == 100 || level == 150 || level == 200)
            return 0;

        if (level < 25)
            return (float)level / 25;
        if (level < 50)
            return (float)(level-25) / 25;
        if (level < 100)
            return (float)(level-50) / 50;
        if (level < 150)
            return (float)(level-100) / 50;
        if (level < 200)
            return (float)(level-150) / 50;

        return 0;
    }

    public bool Purchase()
    {
        // Initial purchase
        // if (startingPrice <= pELiNhaLLitSIJa.Instance.money)
        // {
        //     owned = true;
        //     gm.ChangeMoney(startingPrice * -1d);
        //     level = gm.PurchaseUpgradeItem(_placeId, id, true);
        //     _timer = SetTimer(level);
        //     _currentDuration = _timer;
        //     CalcUpgrade();
        //     return true;
        // }
        return false;
    }

    public double GetCurrentProfitPerSec()
    {
        if (owned)
        {
            return currentProfit / _currentDuration;
        }
        return 0;
    }

	public bool CheckDoubleSpeed() {
		if (level == 25 || level == 50 || level == 100 || level == 150 || level == 200) {
			return true;
		}
		return false;
	}

    public bool Upgrade()
    {
        // int count = pELiNhaLLitSIJa.Instance.getMultiplyCount();
        // if (level + count > GameConsts.Game.maxItemLevel)
        // {
        //     count = GameConsts.Game.maxItemLevel - level;
        // }
        // double price = count * currentPrice;
        // if (price <= pELiNhaLLitSIJa.Instance.money)
        // {
        //     owned = true;
        //     // Add current progress money
        //     gm.ChangeMoney(currentProfit * progress);
        //     gm.ChangeMoney(price * -1d);
        //     for (int i=0; i < count; i++)
        //     {
        //         level = gm.PurchaseUpgradeItem(_placeId, id, i == (count-1));
        //     }
        //     _timer = SetTimer(level);
        //     _currentDuration = _timer;
        //     CalcUpgrade();
        //     return true;
        // }
        return false;
    }

    public void CalcUpgrade(int placeId)
    {
        _placeId = placeId;
        CalcUpgrade();
    }

    public void  CalcUpgrade()
    {
        /*float profitValue;
        float priceValue;

        currentProfit = startingProfit;
        currentPrice = startingPrice;
        for (int i=1; i < level; i++)
        {
			if (level <= 250)
            {
                profitValue = profitCurve.Evaluate(Mathf.Lerp(0, 1, (float)level / 250f));
                priceValue = priceCurve.Evaluate(Mathf.Lerp(0, 1, (float)level / 250f));
            }
            else
            {
                profitValue = profitCurve.Evaluate(1);
                priceValue = priceCurve.Evaluate(1);
            }
            currentPrice *= priceValue;
            currentProfit *= profitValue;

        }*/
        starsCount = GetStarsCount();
        levelProgress = GetLevelProgress();

        getCurrentPrice(level, out currentPrice, out currentProfit);

        string strSuffix1;
        string strMoney1;
        NumToStr.GetNumStr(currentPrice, out strSuffix1, out strMoney1);
        strCurrentPrice = strMoney1;
        strCurrentPriceSuffix = strSuffix1;
        UpdateProfit();
    }

    public void UpdateProfit()
    {
        if (paIkKaHaLlItSiJa.Instance != null && paIkKaHaLlItSiJa.Instance.TryGetWorldProfitForItem(this, out double worldProfit))
        {
            currentProfit = worldProfit;
            strCurrentProfit = NumToStr.GetNumStr(currentProfit);
            return;
        }

        currentProfit = getProfit(level);
        strCurrentProfit = NumToStr.GetNumStr(currentProfit);
    }

    private double getProfit(int lvl)
    {
        float profitValue = profitCurve.Evaluate(lvl);
        double profit = startingProfit * profitValue;
        double kerroin = pELiNhaLLitSIJa.Instance.Kerroin.KerroinArvo;
        //profit = profit + multipler * profit;
        if (kerroin > 0)
        {
            profit = profit * kerroin;
        }
#if SOFTCEN_DEBUG
        //Debug.Log("getProfit: kerroin: " + kerroin + ", profit: " + profit);
#endif
        return profit;
    }

    private void getCurrentPrice(int lvl, out double price, out double profit )
    {
        profit = startingProfit;
        price = startingPrice;
        float priceValue;
        /*for (int i=1; i < lvl; i++)
        {
            price += price * priceIncrement;
            if (i <= 250)
            {
                profitValue = profitCurve.Evaluate(Mathf.Lerp(0, 1, (float)i / 250f));
                priceValue = priceCurve.Evaluate(Mathf.Lerp(0, 1, (float)i / 250f));
            }
            else
            {
                profitValue = profitCurve.Evaluate(1);
                priceValue = priceCurve.Evaluate(1);
            }
            profit *= profitValue;
            price *= priceValue;
        } */
        profit = getProfit(lvl);
        priceValue = priceCurve.Evaluate(lvl);
        price = startingPrice * priceValue;
    }

    public override string ToString()
    {
    return
        $"ImprovementItem(" +
        $"Id={id}, " +
        $"PlaceItem={(placeItem != null ? placeItem.name : "null")}, " +
        $"ImprovementName={improvementName}, " +
        $"StartingPrice={startingPrice}, " +
        $"StartingProfit={startingProfit}, " +
        $"StartingTime={startingTime}, " +
        // $"Owned={owned}, " +
        // $"Level={level}, " +
        // $"Timer={timer}, " +
        // $"CurrentDuration={currentDuration}, " +
        // $"CurrentMoney={currentMoney}, " +
        // $"Progress={progress}, " +
        // $"StrCurrentProfit={strCurrentProfit}, " +
        // $"CurrentProfit={currentProfit}, " +
        // $"StrCurrentPrice={strCurrentPrice}, " +
        // $"StrCurrentPriceSuffix={strCurrentPriceSuffix}, " +
        // $"CurrentPrice={currentPrice}, " +
        // $"StarsCount={starsCount}, " +
        // $"LevelProgress={levelProgress}, " +
        // $"AnalyzeItem={analyzeItem}, " +
        // $"AnalyzeTotalTime={analyzeTotalTime}, " +
        // $"Analyze25Time={analyze25Time}, " +
        // $"Analyze50Time={analyze50Time}, " +
        // $"EffectPool={(effectPool != null ? effectPool.name : "null")}" +
        $")";
    }

    public void AnalyzeItem(double startMoney)
    {
        string dbgStr = "";
        double profit;
        float totalTime = 0;
        float profitTime;
        float levelTime;
        float stepTime = 0;
        double levelMoney;
        double currentMoney = startMoney - startingPrice;
        double curPrice = startingPrice;
        double totalMoney = startingPrice;
        double stepMoney = 0;
        string suffix = "";
        string strMoney = "";
        analyze25Time = 0;
        analyze50Time = 0;
        for (int lvl=1; lvl <= 250; lvl++)
        {
            levelTime = 0;
            levelMoney = 0;
            getCurrentPrice(lvl, out curPrice, out profit);
            profitTime = SetTimer(lvl);
            while (curPrice > currentMoney)
            {
                levelTime += profitTime;
                currentMoney += profit;
                totalMoney += profit;
                levelMoney += profit;
            }
            currentMoney -= curPrice;
            totalTime += levelTime;
            stepTime += levelTime;
            stepMoney += levelMoney;
            if (lvl <= 25)
            {
                analyze25Time += levelTime;
            }
            else if (lvl <= 50)
            {
                analyze50Time += levelTime;
            }
            NumToStr.GetNumStr(totalMoney, out suffix, out strMoney);
            if (lvl == 25 || lvl == 50 || lvl == 100 || lvl == 150 || lvl == 200 || lvl == 250)
            {
                double moneysec = stepMoney / stepTime;
                dbgStr += "Level: " + lvl
                    + ", stepTime: " + NumToStr.GetTimeStr(stepTime)
                    + ", totalTime: " + NumToStr.GetTimeStr(totalTime)
                    + ", levelMoney: " + NumToStr.GetNumStr(levelMoney)
                    + ", totalMoney: " + strMoney + " " + suffix
                    + ", profitTime: " + NumToStr.GetTimeStr(profitTime)
                    + ", curProfit: " + NumToStr.GetNumStr(profit)
                    + ", curPrice: " + NumToStr.GetNumStr(curPrice)
                    + ", money/sec: " + NumToStr.GetNumStr(moneysec)
                    + "\n";
                stepTime = 0;
                stepMoney = 0;
            }
        }
        dbgStr = "Name: " + gameObject.name + ", id: " + id + ", StartMoney: " + startMoney
            + ", TotalTime: " + NumToStr.GetTimeStr(totalTime)
            + ", 25 time: " + NumToStr.GetTimeStr(analyze25Time)
            + ", 50 time: " + NumToStr.GetTimeStr(analyze50Time)
            + "\n" + dbgStr;
        Debug.Log(dbgStr);
        analyzeTotalTime = totalTime;
    }
}
