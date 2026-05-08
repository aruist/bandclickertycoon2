using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldState
{
    public WorldDefinition Definition { get; private set; }

    public double Money { get; private set; }

    public List<RegionState> Regions { get; private set; } = new();

    public WorldState(WorldDefinition definition)
    {
        Definition = definition;
        Money = definition.startingMoney;

        for (int i = 0; i < definition.regions.Count; i++)
        {
            bool unlocked = i == 0;
            Regions.Add(new RegionState(definition.regions[i], unlocked));
        }
    }

    public double CollectAllVaults()
    {
        double amount = 0;
        for (int i=0; i < Regions.Count; i++)
        {
            RegionState region = Regions[i];
            if (region == null) continue;
            double regionVault = region.GetVaultMoney();
            amount += regionVault;
            region.DecreaseVaultMoney(regionVault);
        }
        return amount;
    }

    public void AddMoney(double amount)
    {
        Money += amount;
    }

    public bool CanUnlockRegion(int regionIndex)
    {
        if (regionIndex <= 0 || regionIndex >= Regions.Count)
            return false;

        if (Regions[regionIndex].IsUnlocked)
            return false;

        RegionState previousRegion = Regions[regionIndex - 1];
        RegionDefinition targetRegion = Regions[regionIndex].Definition;

        return previousRegion.Stars >= targetRegion.starsRequiredFromPreviousRegion;
    }

    public double GetRegionValutMoney(int regionId)
    {
        RegionState region = TryToGetRegion(regionId);
        if (region == null) return 0;
        return region.GetVaultMoney();
    }

    public RegionState TryToGetRegion(int regionId)
    {
        if (Regions == null) return null;
        for (int i = 0; i < Regions.Count; i++)
        {
            RegionState region = Regions[i];
            if (region == null || region.Definition == null) continue;
            if (region.Definition.id == regionId) return region;
        }
        return null;
    }

    public bool TryUnlockRegion(int regionIndex)
    {
        if (!CanUnlockRegion(regionIndex))
            return false;

        Regions[regionIndex].Unlock();
        return true;
    }

    public bool TryBuyImprovement(int regionIndex, int improvementIndex)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count)
            return false;

        double money = Money;

        bool success = Regions[regionIndex].TryBuyImprovement(improvementIndex, ref money);

        if (!success)
            return false;

        Money = money;
        return true;
    }

    public int GetRegionImprovementLevel(int regionId, int improvementId)
    {
        if (Regions == null) return 0;
        RegionState region = TryToGetRegion(regionId);
        if (region == null) return 0;
        return region.GetImprovementLevel(improvementId);
    }

    public bool SetImprovementLevel(int regionIndex, int improvementIndex, int level, bool resetProgress = true)
    {
        if (regionIndex < 0 || regionIndex >= Regions.Count)
            return false;

        return Regions[regionIndex].SetImprovementLevel(improvementIndex, level, resetProgress);
    }

    public void UnlockAllRegions()
    {
        for (int i = 0; i < Regions.Count; i++)
        {
            Regions[i].SetUnlocked(true);
        }
    }

    public void Tick(double deltaSeconds)
    {
        double earned = 0;

        foreach (RegionState region in Regions)
        {
            earned += region.Tick(deltaSeconds);
        }

        AddMoney(earned);
    }
}
