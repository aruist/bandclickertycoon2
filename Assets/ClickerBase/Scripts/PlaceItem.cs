using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;
using System.Runtime.ExceptionServices;

public class PlaceItem : MonoBehaviour {
    public static event Action OnCollectedMoneyChanged;

    public int id;
    public string PlaceName;
    public double startingPrice;
    [SerializeField] private double money;

    public float wantedDuration = 0;

    public List<ImprovementItem> improvements;
    public double startMoney = 4;
    public double startMultiply = 5;
    public double startProfit = 1;
    public double startProfitMultiply = 5;
    public double[] kassaKaappiKoot;

    public bool analyze = false;
    public bool analyzeChangeValues = false;
    public string reSURssINImi;
    public int toLevel = 26;
    public bool Automate = false;
    public float AutomateDesiredDuration = 1200f;
    public int improvementTargetLevel = 1;

    public float posY = 0f;

    void Start()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("PlaceItem - Start", gameObject);
        #endif
        if (improvements.Count == 0) TryToGetImprovements();
        if (Automate)
            AutomateImprovementStartingValues();
        InitializeImprovements();
        if (analyze)
        {
            AnalyzePlace();
        }
    }

    [ContextMenu("Analyze Place")]
    public void AnalyzePlace()
    {
        List<ImprovementItem> items = new List<ImprovementItem>();
        for (int i = 0; i < transform.childCount; i++)
        {
            ImprovementItem item = transform.GetChild(i).GetComponent<ImprovementItem>();
            if (item != null)
                items.Add(item);
        }

        if (items.Count == 0)
        {
            Debug.LogWarning("AnalyzePlace: no improvements found for " + PlaceName, gameObject);
            return;
        }

        items.Sort((a, b) => a.id.CompareTo(b.id));
        int targetLevel = Mathf.Max(1, toLevel);

        double startMoneyLocal = Math.Max(0.001d, startMoney);
        double moneyLocal = startMoneyLocal;
        double timeSec = 0d;
        double totalSpent = 0d;

        int count = items.Count;
        int[] levels = new int[count];
        bool[] owned = new bool[count];
        double[] nextTickSec = new double[count];
        for (int i = 0; i < count; i++)
            nextTickSec[i] = double.PositiveInfinity;

        int safety = 0;
        const int maxSteps = 2000000;

        while (!AllReachedLevel(levels, targetLevel))
        {
            safety++;
            if (safety > maxSteps)
            {
                Debug.LogWarning("AnalyzePlace: simulation stopped by safety guard for " + PlaceName, gameObject);
                break;
            }

            bool purchasedAtThisTime = false;
            while (true)
            {
                int bestIndex = -1;
                double bestScore = -1d;
                double bestCost = 0d;

                for (int i = 0; i < count; i++)
                {
                    ImprovementItem imp = items[i];
                    if (levels[i] >= targetLevel)
                        continue;

                    int nextLevel = owned[i] ? levels[i] + 1 : 1;
                    double cost = owned[i] ? GetUpgradePriceForLevel(imp, levels[i]) : imp.startingPrice;
                    if (cost > moneyLocal || cost <= 0d)
                        continue;

                    double oldPps = owned[i] ? GetProfitPerSecondAtLevel(imp, levels[i]) : 0d;
                    double newPps = GetProfitPerSecondAtLevel(imp, nextLevel);
                    double gain = Math.Max(0d, newPps - oldPps);
                    double score = gain / Math.Max(cost, 0.000001d);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                        bestCost = cost;
                    }
                }

                if (bestIndex < 0)
                    break;

                ImprovementItem buyImp = items[bestIndex];
                moneyLocal -= bestCost;
                totalSpent += bestCost;
                owned[bestIndex] = true;
                levels[bestIndex] = owned[bestIndex] ? Mathf.Max(1, levels[bestIndex] + 1) : 1;
                nextTickSec[bestIndex] = timeSec + GetDurationForLevel(buyImp, levels[bestIndex]);
                purchasedAtThisTime = true;
            }

            if (AllReachedLevel(levels, targetLevel))
                break;

            double nextEvent = double.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (owned[i] && nextTickSec[i] < nextEvent)
                    nextEvent = nextTickSec[i];
            }

            if (double.IsInfinity(nextEvent))
                break;

            timeSec = nextEvent;
            for (int i = 0; i < count; i++)
            {
                if (!owned[i])
                    continue;

                if (Math.Abs(nextTickSec[i] - timeSec) < 0.0000001d)
                {
                    moneyLocal += GetProfitAtLevel(items[i], levels[i]);
                    nextTickSec[i] = timeSec + GetDurationForLevel(items[i], levels[i]);
                }
            }

            if (!purchasedAtThisTime && double.IsInfinity(nextEvent))
                break;
        }

        double ppsL1 = GetPlaceProfitPerSecondAtUniformLevel(items, 1);
        double ppsL2 = GetPlaceProfitPerSecondAtUniformLevel(items, Mathf.Max(1, Mathf.RoundToInt(targetLevel * 0.4f)));
        double ppsL3 = GetPlaceProfitPerSecondAtUniformLevel(items, Mathf.Max(1, Mathf.RoundToInt(targetLevel * 0.7f)));
        double ppsL4 = GetPlaceProfitPerSecondAtUniformLevel(items, targetLevel);

        // Target fill durations: 5 min, 15 min, 30 min, 60 min.
        double v1 = NiceNumber(ppsL1 * 300d);
        double v2 = NiceNumber(ppsL2 * 900d);
        double v3 = NiceNumber(ppsL3 * 1800d);
        double v4 = NiceNumber(ppsL4 * 3600d);

        string strTime = NumToStr.GetTimeStr(timeSec);
        string strSpent = NumToStr.GetNumStr(totalSpent);
        string strPpsTarget = NumToStr.GetNumStr(ppsL4);
        string strV1 = NumToStr.GetNumStr(v1);
        string strV2 = NumToStr.GetNumStr(v2);
        string strV3 = NumToStr.GetNumStr(v3);
        string strV4 = NumToStr.GetNumStr(v4);

        Debug.Log(
            "AnalyzePlace: " + PlaceName
            + ", targetLevel: " + targetLevel
            + ", startMoney: " + NumToStr.GetNumStr(startMoneyLocal)
            + ", estPlayTime: " + strTime
            + ", estTotalSpent: " + strSpent
            + ", estPPS@" + targetLevel + ": " + strPpsTarget
            + "\nPreferredVaultSizes(4): ["
            + strV1 + ", " + strV2 + ", " + strV3 + ", " + strV4 + "]",
            gameObject);
    }

    private static bool AllReachedLevel(int[] levels, int targetLevel)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] < targetLevel)
                return false;
        }
        return true;
    }

    private static double GetPlaceProfitPerSecondAtUniformLevel(List<ImprovementItem> items, int level)
    {
        double pps = 0d;
        for (int i = 0; i < items.Count; i++)
            pps += GetProfitPerSecondAtLevel(items[i], level);
        return pps;
    }

    private static double GetProfitPerSecondAtLevel(ImprovementItem imp, int level)
    {
        if (imp == null || level <= 0)
            return 0d;

        double profit = GetProfitAtLevel(imp, level);
        double duration = GetDurationForLevel(imp, level);
        if (duration <= 0d)
            return 0d;
        return profit / duration;
    }

    private static double GetProfitAtLevel(ImprovementItem imp, int level)
    {
        if (imp == null)
            return 0d;

        float mul = 1f;
        if (imp.profitCurve != null)
            mul = imp.profitCurve.Evaluate(level);
        return imp.startingProfit * mul;
    }

    private static double GetUpgradePriceForLevel(ImprovementItem imp, int currentLevel)
    {
        if (imp == null)
            return 0d;

        float mul = 1f;
        if (imp.priceCurve != null)
            mul = imp.priceCurve.Evaluate(currentLevel);
        return imp.startingPrice * mul;
    }

    private static double GetDurationForLevel(ImprovementItem imp, int level)
    {
        if (imp == null)
            return 1d;

        float d;
        if (level < 25)
            d = imp.startingTime;
        else if (level < 50)
            d = imp.startingTime / 2f;
        else if (level < 100)
            d = imp.startingTime / 4f;
        else if (level < 150)
            d = imp.startingTime / 8f;
        else if (level < 200)
            d = imp.startingTime / 16f;
        else
            d = imp.startingTime / 32f;

        if (d < 1f)
            d = 1f;
        return d;
    }

    private static double NiceNumber(double value)
    {
        if (value <= 0d)
            return 0d;

        double exp = Math.Floor(Math.Log10(value));
        double factor = Math.Pow(10d, exp);
        double baseVal = value / factor;
        double rounded;
        if (baseVal <= 1d)
            rounded = 1d;
        else if (baseVal <= 2d)
            rounded = 2d;
        else if (baseVal <= 3d)
            rounded = 3d;
        else if (baseVal <= 4d)
            rounded = 4d;
        else if (baseVal <= 5d)
            rounded = 5d;
        else
            rounded = 10d;

        return rounded * factor;
    }

    void OnEnable()
    {
        double cap = GetVaultCap();
        if (money > cap)
            money = cap;
        SyncVaultMoneyToPlayerData();

        if (OnCollectedMoneyChanged != null)
        {
            OnCollectedMoneyChanged();
        }
    }

    public double Money => money;
    public bool IsVaultFull => money >= GetVaultCap();

    public int GetVaultLevelIndex()
    {
        if (pELiNhaLLitSIJa.Instance == null || pELiNhaLLitSIJa.Instance.playerData == null)
            return 0;

        return pELiNhaLLitSIJa.Instance.playerData.GetVaultOwned(id);
    }

    public double GetVaultCap()
    {
        if (kassaKaappiKoot == null || kassaKaappiKoot.Length == 0)
            return double.MaxValue;

        int idx = GetVaultLevelIndex();
        if (idx < 0) idx = 0;
        if (idx >= kassaKaappiKoot.Length) idx = kassaKaappiKoot.Length - 1;

        return Math.Max(0d, kassaKaappiKoot[idx]);
    }

    public double AddMoneyToVault(double amount)
    {
        if (amount <= 0d)
            return 0d;

        double before = money;
        double cap = GetVaultCap();
        if (before >= cap)
            return 0d;

        double next = before + amount;
        if (next > cap)
            next = cap;

        double accepted = next - before;
        if (accepted > 0d)
        {
            money = next;
            SyncVaultMoneyToPlayerData();
            if (OnCollectedMoneyChanged != null)
                OnCollectedMoneyChanged();
        }

        return accepted;
    }

    public void DecreaseMoney(double amount)
    {
        double before = money;
        money -= amount;
        if (money < 0)
            money = 0;
        SyncVaultMoneyToPlayerData();
        if (money != before && OnCollectedMoneyChanged != null)
            OnCollectedMoneyChanged();

    }

    public void ChangeMoney(double amount)
    {
        if (amount > 0d)
        {
            AddMoneyToVault(amount);
        }
        else if (amount < 0d)
        {
            DecreaseMoney(-amount);
        }
    }

    public void Analyze()
    {
        #if SOFTCEN_DEBUG
        Debug.Log("PlaceItem Analyze");
        #endif
        float totalTime = 0;
        float total25Time = 0;
        float total50Time = 0;
        for (int i = 0; i < improvements.Count; i++)
        {
            if (analyzeChangeValues)
            {
                if (i > 1)
                {
                    improvements[i].startingPrice = startMoney + startMultiply * improvements[i - 1].startingPrice;
                    improvements[i].startingProfit = startProfit + startProfitMultiply * improvements[i - 1].startingProfit;
                }
                else
                {
                    improvements[i].startingPrice = startMoney;
                    improvements[i].startingProfit = startProfit;
                }
            }
            improvements[i].AnalyzeItem(improvements[i].startingPrice);
            totalTime += improvements[i].analyzeTotalTime;
            total25Time += improvements[i].analyze25Time;
            total50Time += improvements[i].analyze50Time;
        }
        Debug.Log("Place totalTime: " + NumToStr.GetTimeStr(totalTime)
            + ", 25Time: " + NumToStr.GetTimeStr(total25Time)
            + ", 50Time: " + NumToStr.GetTimeStr(total50Time)
            );

        if (wantedDuration > 0)
        {
            if (totalTime > wantedDuration)
            {
                // Lower values

            }
        }

    }

    public double GetProfitPerSec()
    {
        double profit = 0;
        for (int i=0; i < improvements.Count; i++)
        {
            if (improvements[i].owned)
            {
                improvements[i].UpdateProfit();
                profit += improvements[i].GetCurrentProfitPerSec();
            }
        }
        return profit;
    }

    [ContextMenu("Automate Improvement Values")]
    public void AutomateImprovementStartingValues()
    {
        #if UNITY_EDITOR
        Undo.RecordObject(this, "Automate Improvement Values");
        #endif

        if (improvements == null || improvements.Count == 0)
            TryToGetImprovements();

        if (improvements == null || improvements.Count == 0)
        {
            Debug.LogWarning("AutomateImprovementStartingValues: no improvements found for " + PlaceName, gameObject);
            return;
        }
        #if UNITY_EDITOR
        for (int i=0; i < improvements.Count; i++)
        {
            Undo.RecordObject(improvements[i], "Automate Improvement Values");
        }
        #endif

        List<ImprovementItem> items = new List<ImprovementItem>(improvements);
        items.RemoveAll(i => i == null);
        if (items.Count == 0)
            return;

        items.Sort((a, b) => a.id.CompareTo(b.id));

        float desiredSec = Mathf.Max(60f, AutomateDesiredDuration);
        int n = items.Count;
        int targetLvl = Mathf.Max(1, improvementTargetLevel);
        string report = "AutomateImprovementStartingValues: " + PlaceName
            + ", desiredDurationSec: " + desiredSec
            + ", targetLevel: " + targetLvl + "\n";

        double startMoneyLocal = Math.Max(0.001d, startMoney);
        BuildInitialValues(items, startMoneyLocal);
        TuneForOrderAndDuration(items, targetLvl, desiredSec, startMoneyLocal, ref report);

        for (int i = 0; i < n; i++)
        {
            ImprovementItem item = items[i];
            report += "id: " + item.id
                + ", price: " + NumToStr.GetNumStr(item.startingPrice)
                + ", profit: " + NumToStr.GetNumStr(item.startingProfit)
                + ", time: " + item.startingTime.ToString("F1")
                + "\n";
        }

        Debug.Log(report, gameObject);
        #if UNITY_EDITOR
        for (int i=0; i < improvements.Count; i++)
        {
            EditorUtility.SetDirty(improvements[i]);
        }
        EditorUtility.SetDirty(this);
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        #endif
    }

    private static void BuildInitialValues(List<ImprovementItem> items, double startMoneyLocal)
    {
        #if SOFTCEN_DEBUG
        Debug.Log("PlaceItem BuildInitialValues");
        #endif

        if (items == null || items.Count == 0)
            return;

        int n = items.Count;
        const float minStartingTime = 4f;
        const float maxStartingTime = 60f;
        const float timeCurvePower = 1.1f;
        const float roiMinSec = 40f;
        const float roiMaxSec = 220f;
        const double unlockMultiplier = 1.35d;

        for (int i = 0; i < n; i++)
        {
            ImprovementItem item = items[i];
            if (item == null)
                continue;

            float norm = n > 1 ? (float)i / (n - 1) : 0f;
            float tNorm = Mathf.Pow(norm, timeCurvePower);
            item.startingTime = Mathf.Lerp(minStartingTime, maxStartingTime, tNorm);

            if (i == 0)
                item.startingPrice = Math.Max(0.001d, startMoneyLocal);
            else
                item.startingPrice = 0d;
        }

        for (int i = 0; i < n; i++)
        {
            ImprovementItem item = items[i];
            if (item == null)
                continue;

            if (i > 0)
            {
                ImprovementItem prev = items[i - 1];
                double prevCostTo25 = EstimateCostToLevel(prev, 25);
                item.startingPrice = NiceNumber(Math.Max(1d, prevCostTo25 * unlockMultiplier));
            }

            float norm = n > 1 ? (float)i / (n - 1) : 0f;
            float roiSec = Mathf.Lerp(roiMinSec, roiMaxSec, norm);
            item.startingProfit = (item.startingPrice * item.startingTime) / roiSec;
        }
    }

    private static void TuneForOrderAndDuration(List<ImprovementItem> items, int targetLevel, float desiredSec, double startMoneyLocal, ref string report)
    {
        if (items == null || items.Count == 0)
            return;

        const int maxIterations = 18;
        const double orderGuard = 1.10d;
        for (int iter = 0; iter < maxIterations; iter++)
        {
            SimulationResult sim = SimulateProgress(items, targetLevel, startMoneyLocal);
            if (!sim.Success || double.IsInfinity(sim.TotalTimeSec) || sim.TotalTimeSec <= 0d)
            {
                bool recovered = TryRecoverFromSimulationFailure(items, targetLevel, startMoneyLocal);
                if (!recovered)
                {
                    report += "Solver stopped: simulation failed at iter " + iter + "\n";
                    break;
                }
                report += "Solver recovered from simulation failure at iter " + iter + "\n";
                continue;
            }

            bool fixedAnyOrder = false;
            for (int i = 0; i < items.Count - 1; i++)
            {
                if (sim.TimeReachedTargetByItem[i] + 0.000001d < sim.TimeFirstOwnedByItem[i + 1])
                    continue;

                items[i + 1].startingPrice = NiceNumber(items[i + 1].startingPrice * 1.08d);
                fixedAnyOrder = true;
            }

            if (fixedAnyOrder)
                continue;

            double ratio = sim.TotalTimeSec / Math.Max(1d, desiredSec);
            if (ratio > 0.92d && ratio < 1.08d)
            {
                report += "Solver converged at iter " + iter + ", timeSec: " + sim.TotalTimeSec.ToString("F1") + "\n";
                break;
            }

            // Damped convergence to avoid runaway values.
            double profitScale = Math.Pow(ratio, 0.22d);
            if (profitScale < 0.88d) profitScale = 0.88d;
            if (profitScale > 1.14d) profitScale = 1.14d;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;
                items[i].startingProfit *= profitScale;
            }

            // Keep ROI safeguard vs next unlock while current < target.
            EnforceRoiSafety(items, targetLevel, orderGuard);
        }
    }

    private static bool TryRecoverFromSimulationFailure(List<ImprovementItem> items, int targetLevel, double startMoneyLocal)
    {
        if (items == null || items.Count == 0)
            return false;

        const int maxRecoveryPasses = 10;
        for (int pass = 0; pass < maxRecoveryPasses; pass++)
        {
            if (items[0] != null)
                items[0].startingPrice = Math.Min(items[0].startingPrice, Math.Max(0.01d, startMoneyLocal));

            for (int i = 1; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;
                items[i].startingPrice = NiceNumber(Math.Max(1d, items[i].startingPrice * 0.82d));
            }

            SimulationResult retry = SimulateProgress(items, targetLevel, startMoneyLocal);
            if (retry.Success && !double.IsInfinity(retry.TotalTimeSec) && retry.TotalTimeSec > 0d)
                return true;
        }

        return false;
    }

    private static void EnforceRoiSafety(List<ImprovementItem> items, int targetLevel, double guardMultiplier)
    {
        #if SOFTCEN_DEBUG
        Debug.Log("PlaceItem EnforceRoiSafety");
        #endif
        int n = items.Count;
        for (int i = 0; i < n - 1; i++)
        {
            ImprovementItem cur = items[i];
            ImprovementItem next = items[i + 1];
            if (cur == null || next == null)
                continue;

            int levelToCheck = Math.Max(1, targetLevel - 1);
            double upgradeCost = GetUpgradePriceForLevel(cur, levelToCheck);
            double curPps = GetProfitPerSecondAtLevel(cur, levelToCheck);
            double curNextPps = GetProfitPerSecondAtLevel(cur, levelToCheck + 1);
            double upgradeRoi = (curNextPps - curPps) / Math.Max(upgradeCost, 0.000001d);

            double unlockCost = next.startingPrice;
            double unlockPpsGain = GetProfitPerSecondAtLevel(next, 1);
            double unlockRoi = unlockPpsGain / Math.Max(unlockCost, 0.000001d);

            if (upgradeRoi < unlockRoi * guardMultiplier)
            {
                next.startingPrice = NiceNumber(next.startingPrice * 1.10d);
            }

            // Stronger guard: unlocking the next improvement should not beat
            // the full investment path of pushing current to target level.
            double curCostToTarget = EstimateCostToLevel(cur, targetLevel);
            double curPpsAtTarget = GetProfitPerSecondAtLevel(cur, targetLevel);
            double curPathRoi = curPpsAtTarget / Math.Max(curCostToTarget, 0.000001d);
            double unlockPathRoi = GetProfitPerSecondAtLevel(next, 1) / Math.Max(next.startingPrice, 0.000001d);

            if (unlockPathRoi > curPathRoi * 0.95d)
            {
                next.startingPrice = NiceNumber(next.startingPrice * 1.15d);
            }
        }
    }

    private static double EstimateCostToLevel(ImprovementItem item, int targetLevel)
    {
        if (item == null)
            return 0d;

        int lvl = Math.Max(1, targetLevel);
        double total = Math.Max(0d, item.startingPrice);
        for (int l = 1; l < lvl; l++)
            total += Math.Max(0d, GetUpgradePriceForLevel(item, l));
        return total;
    }

    private static SimulationResult SimulateProgress(List<ImprovementItem> items, int targetLevel, double startingMoneySim)
    {
        if (items == null || items.Count == 0)
            return SimulationResult.Fail();

        int count = items.Count;
        double moneyLocal = Math.Max(0d, startingMoneySim);
        double timeSec = 0d;
        int[] levels = new int[count];
        bool[] owned = new bool[count];
        double[] firstOwnedTime = new double[count];
        double[] reachedTargetTime = new double[count];
        double[] nextTickSec = new double[count];
        for (int i = 0; i < count; i++)
        {
            nextTickSec[i] = double.PositiveInfinity;
            firstOwnedTime[i] = double.PositiveInfinity;
            reachedTargetTime[i] = double.PositiveInfinity;
        }

        int safety = 0;
        const int maxSteps = 1500000;

        while (!AllReachedLevel(levels, targetLevel))
        {
            safety++;
            if (safety > maxSteps)
                return SimulationResult.Fail();

            while (true)
            {
                int bestIndex = -1;
                double bestScore = -1d;
                double bestCost = 0d;

                for (int i = 0; i < count; i++)
                {
                    ImprovementItem imp = items[i];
                    if (imp == null || levels[i] >= targetLevel)
                        continue;

                    if (!IsPurchaseAllowedByStrategy(levels, owned, i, targetLevel))
                        continue;

                    int nextLevel = owned[i] ? levels[i] + 1 : 1;
                    double cost = owned[i] ? GetUpgradePriceForLevel(imp, levels[i]) : imp.startingPrice;
                    if (cost > moneyLocal || cost <= 0d)
                        continue;

                    double oldPps = owned[i] ? GetProfitPerSecondAtLevel(imp, levels[i]) : 0d;
                    double newPps = GetProfitPerSecondAtLevel(imp, nextLevel);
                    double gain = Math.Max(0d, newPps - oldPps);
                    double score = gain / Math.Max(cost, 0.000001d);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                        bestCost = cost;
                    }
                }

                if (bestIndex < 0)
                    break;

                ImprovementItem buyImp = items[bestIndex];
                moneyLocal -= bestCost;
                owned[bestIndex] = true;
                if (double.IsInfinity(firstOwnedTime[bestIndex]))
                    firstOwnedTime[bestIndex] = timeSec;
                levels[bestIndex] = Mathf.Max(1, levels[bestIndex] + 1);
                if (levels[bestIndex] >= targetLevel && double.IsInfinity(reachedTargetTime[bestIndex]))
                    reachedTargetTime[bestIndex] = timeSec;
                nextTickSec[bestIndex] = timeSec + GetDurationForLevel(buyImp, levels[bestIndex]);
            }

            if (AllReachedLevel(levels, targetLevel))
                break;

            double nextEvent = double.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (owned[i] && nextTickSec[i] < nextEvent)
                    nextEvent = nextTickSec[i];
            }

            if (double.IsInfinity(nextEvent))
                return SimulationResult.Fail();

            timeSec = nextEvent;
            for (int i = 0; i < count; i++)
            {
                if (!owned[i])
                    continue;

                if (Math.Abs(nextTickSec[i] - timeSec) < 0.0000001d)
                {
                    moneyLocal += GetProfitAtLevel(items[i], levels[i]);
                    nextTickSec[i] = timeSec + GetDurationForLevel(items[i], levels[i]);
                }
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (double.IsInfinity(reachedTargetTime[i]) && levels[i] >= targetLevel)
                reachedTargetTime[i] = timeSec;
            if (double.IsInfinity(firstOwnedTime[i]) && levels[i] > 0)
                firstOwnedTime[i] = timeSec;
        }

        return new SimulationResult
        {
            Success = true,
            TotalTimeSec = timeSec,
            TimeFirstOwnedByItem = firstOwnedTime,
            TimeReachedTargetByItem = reachedTargetTime
        };
    }

    private static bool IsPurchaseAllowedByStrategy(int[] levels, bool[] owned, int index, int targetLevel)
    {
        // Upgrades on already owned improvement are always allowed.
        if (owned[index])
            return true;

        // First improvement can always be unlocked.
        if (index <= 0)
            return true;

        // Sequential-gate unlocks: next improvement can be unlocked only
        // after previous one has reached target level.
        int required = Math.Max(1, targetLevel);
        return levels[index - 1] >= required;
    }

    private class SimulationResult
    {
        public bool Success;
        public double TotalTimeSec;
        public double[] TimeFirstOwnedByItem;
        public double[] TimeReachedTargetByItem;

        public static SimulationResult Fail()
        {
            return new SimulationResult
            {
                Success = false,
                TotalTimeSec = double.PositiveInfinity,
                TimeFirstOwnedByItem = new double[0],
                TimeReachedTargetByItem = new double[0]
            };
        }
    }

    public string ImprovementsToString(string header)
    {
        string str = header;
        if (improvements == null) return str;

        for (int i=0; i < improvements.Count; i++)
        {
            if (improvements[i] == null) continue;
            str += $"{i}, " + improvements.ToString() + "\n";
        }
        return str;
    }

    private void TryToGetImprovements()
    {
        if (improvements == null) improvements = new List<ImprovementItem>();
        if (improvements.Count > 0) return;
        for (int i = 0; i < transform.childCount; i++)
        {
            ImprovementItem item = transform.GetChild(i).GetComponent<ImprovementItem>();
            if (item != null)
            {
                //PlaceData pd = pELiNhaLLitSIJa.Instance.playerData.GetPlaceData(id);
                // int itemLevel = pELiNhaLLitSIJa.Instance.playerData.GetPlaceItemLevel(id, item.id);
                // if (itemLevel > 0)
                //     item.owned = true;
                // item.level = itemLevel;
                // item.placeItem = this;
                // item.CalcUpgrade(id);
                improvements.Add(item);
            }
        }
    }

    private void InitializeImprovements()
    {
        pELiNhaLLitSIJa pELiNhaLLitSIJa = pELiNhaLLitSIJa.Instance;
        if (pELiNhaLLitSIJa == null || pELiNhaLLitSIJa.playerData == null || improvements == null || improvements.Count <= 0) return;
        PlaceData pd = pELiNhaLLitSIJa.playerData.GetPlaceData(id);
        money = Math.Max(0d, pd.vaultMoney);
        if (money > GetVaultCap())
            money = GetVaultCap();
        pd.vaultMoney = money;
        for (int i = 0; i < improvements.Count; i++)
        {
            var item = improvements[i];
            if (item == null) continue;
            int itemLevel = pELiNhaLLitSIJa.playerData.GetPlaceItemLevel(id, item.id);
            item.level = itemLevel;
            if (itemLevel > 0)
                item.owned = true;
            item.placeItem = this;
            item.placeItem = this;
            item.CalcUpgrade(id);
        }

    }

    private void SyncVaultMoneyToPlayerData()
    {
        if (pELiNhaLLitSIJa.Instance == null || pELiNhaLLitSIJa.Instance.playerData == null)
            return;
        pELiNhaLLitSIJa.Instance.playerData.SetRegionVaultMoney(id, money);
    }

}
