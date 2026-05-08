using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ImprovementState
{
    public ImprovementDefinition Definition { get; private set; }

    public int Level { get; private set; }
    public double ProgressSeconds { get; private set; }

    public bool IsUnlocked => Level > 0;

    public int Stars => Definition.GetStarsAtLevel(Level);

    public ImprovementState(ImprovementDefinition definition)
    {
        Definition = definition;
        Level = 0;
        ProgressSeconds = 0;
    }

    public void SetLevel(int level, bool resetProgress = true)
    {
        Level = Mathf.Clamp(level, 0, ImprovementDefinition.MaxLevel);
        if (resetProgress)
            ProgressSeconds = 0;
    }

    public void SetProgressSeconds(double progressSeconds)
    {
        double duration = GetCurrentDuration();
        if (duration <= 0)
        {
            ProgressSeconds = 0;
            return;
        }

        ProgressSeconds = Math.Max(0, progressSeconds % duration);
    }

    public bool CanBuy(double money)
    {
        if (Level >= ImprovementDefinition.MaxLevel)
            return false;

        return money >= GetNextUpgradeCost();
    }

    public bool TryBuy(ref double money)
    {
        if (!CanBuy(money))
            return false;

        money -= GetNextUpgradeCost();
        Level++;

        return true;
    }

    public double GetNextUpgradeCost()
    {
        return Definition.GetUpgradeCost(Level);
    }

    public double GetCurrentProfit()
    {
        return Definition.GetProfitAtLevel(Level);
    }

    public double GetCurrentDuration()
    {
        return Definition.GetDurationAtLevel(Level);
    }

    public float GetLevelProgress()
    {
        if (Level == 0 || Level == 1 || Level == 25 || Level == 50 || Level == 100 || Level == 150 || Level == 200)
            return 0;

        if (Level < 25) return (float)Level / 25;
        if (Level < 50) return (float)(Level-25) / 25;
        if (Level < 100) return (float)(Level-50) / 50;
        if (Level < 150) return (float)(Level-100) / 50;
        if (Level < 200) return (float)(Level-150) / 50;
        return 0;
    }

    public float GetProgress()
    {
        double duration = GetCurrentDuration();
        if (duration == 0) return 0;
        double progress = ProgressSeconds / duration;
        if (progress < 0) progress = 0;
        else if (progress > 1) progress = 1;
        return (float)progress;
    }

    public float GetTimeLeft()
    {
        double duration = GetCurrentDuration();
        double timeLeft = duration - ProgressSeconds;
        return (float)timeLeft;
    }

    public double Tick(double deltaSeconds)
    {
        if (!IsUnlocked)
            return 0;

        double duration = GetCurrentDuration();

        if (duration <= 0)
            return 0;

        ProgressSeconds += deltaSeconds;

        int completedCycles = (int)(ProgressSeconds / duration);

        if (completedCycles <= 0)
            return 0;

        ProgressSeconds -= completedCycles * duration;

        return completedCycles * GetCurrentProfit();
    }
}