using System;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Idle Game/Improvement Definition")]
public class ImprovementDefinition : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string displayName;
    public LocalizedString localizedDisplayName;
    public LocalizedString localizedCardTitle;
    public LocalizedString localizedCardDescription;

    [Header("Base Economy")]
    public double startingPrice = 10;
    public double startingProfit = 1;
    public double startingDurationSeconds = 5;

    [Header("Growth")]
    [Tooltip("Upgrade price multiplier. Example: 1.13 means every upgrade costs 13% more.")]
    public double priceGrowth = 1.13;

    [Tooltip("Profit multiplier. Example: 1.07 means every upgrade gives 7% more profit.")]
    public double profitGrowth = 1.07;

    [Header("Visuals")]
    public Sprite sprite;

    public const int MaxLevel = 200;

    public double GetUpgradeCost(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, 0, MaxLevel);

        // Level 0 -> 1 costs startingPrice.
        // Level 1 -> 2 costs startingPrice * priceGrowth.
        return startingPrice * Math.Pow(priceGrowth, currentLevel);
    }

    public double GetProfitAtLevel(int level)
    {
        if (level <= 0)
            return 0;

        level = Mathf.Clamp(level, 1, MaxLevel);

        // Level 1 gives startingProfit.
        // Level 2 gives startingProfit * profitGrowth.
        return startingProfit * Math.Pow(profitGrowth, level - 1);
    }

    public double GetDurationAtLevel(int level)
    {
        if (level <= 0)
            return startingDurationSeconds;

        if (level < 25)
            return startingDurationSeconds;

        if (level < 50)
            return startingDurationSeconds / 2.0;

        if (level < 100)
            return startingDurationSeconds / 4.0;

        if (level < 150)
            return startingDurationSeconds / 8.0;

        if (level < 200)
            return startingDurationSeconds / 16.0;

        return startingDurationSeconds / 32.0;
    }

    public int GetStarsAtLevel(int level)
    {
        if (level >= 200) return 5;
        if (level >= 150) return 4;
        if (level >= 100) return 3;
        if (level >= 50) return 2;
        if (level >= 25) return 1;

        return 0;
    }

    public double GetTotalCostToReachLevel(int targetLevel)
    {
        targetLevel = Mathf.Clamp(targetLevel, 1, MaxLevel);

        double total = 0;

        for (int currentLevel = 0; currentLevel < targetLevel; currentLevel++)
        {
            total += GetUpgradeCost(currentLevel);
        }

        return total;
    }

    public void AutoBalanceProfitForTargetTime(
        double targetSecondsToReachLevel25,
        double costCoverageMultiplier = 1.15)
    {
        const int targetLevel = 25;

        if (targetSecondsToReachLevel25 <= 0)
        {
            Debug.LogWarning($"{name}: targetSecondsToReachLevel25 must be greater than zero.");
            return;
        }

        double totalCostToLevel25 = GetTotalCostToReachLevel(targetLevel);

        double incomeRateIfStartingProfitIsOne = 0;

        for (int level = 1; level < targetLevel; level++)
        {
            double profitMultiplier = Math.Pow(profitGrowth, level - 1);
            double duration = GetDurationAtLevel(level);

            incomeRateIfStartingProfitIsOne += profitMultiplier / duration;
        }

        incomeRateIfStartingProfitIsOne /= targetLevel - 1;

        double requiredProfit =
            totalCostToLevel25 *
            costCoverageMultiplier /
            targetSecondsToReachLevel25 /
            incomeRateIfStartingProfitIsOne;

        startingProfit = NiceNumber(requiredProfit);
    }

    public static double NiceNumber(double value)
    {
        if (value <= 0)
            return 0;

        double exponent = Math.Floor(Math.Log10(value));
        double fraction = value / Math.Pow(10, exponent);

        double niceFraction;

        if (fraction < 1.5)
            niceFraction = 1;
        else if (fraction < 2.5)
            niceFraction = 2;
        else if (fraction < 5)
            niceFraction = 5;
        else
            niceFraction = 10;

        return niceFraction * Math.Pow(10, exponent);
    }
}