using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegionState
{
    public RegionDefinition Definition { get; private set; }
    public List<ImprovementState> Improvements { get; private set; } = new();
    public bool IsUnlocked { get; private set; }
    public SecureDoubleLight money { get; private set; }

    public RegionState(RegionDefinition definition, bool unlocked)
    {
        Definition = definition;
        IsUnlocked = unlocked;

        foreach (ImprovementDefinition improvementDefinition in definition.improvements)
        {
            Improvements.Add(new ImprovementState(improvementDefinition));
        }
    }

    public double GetStartingMoney()
    {
        if (Definition == null) return 0d;
        return Definition.firstImprovementPrice;
    }

    public double AddMoneyToVault(double amount)
    {
         pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || Definition == null) return 0;

        if (amount <= 0d) return 0d;

        int id = Definition.id;
        double before = ph.GetRegionVaultMoney(id);
        double cap = GetVaultCap();
        if (before >= cap)
            return 0d;

        double next = before + amount;
        if (next > cap)
            next = cap;

        double accepted = next - before;
        if (accepted > 0d)
        {
            Debug.Log($"AddMoneyToVault {id}: {amount}, {next}, accepted: {accepted} ");
            ph.ChangeRegionVaultMoney(id, accepted);
        }
        return accepted;
    }

    public double GetVaultMoney()
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;

        if (ph == null || Definition == null) return 0d;
        return ph.GetRegionVaultMoney(Definition.id);
    }

    private double GetVaultCap()
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || Definition == null) return 0;
        int vaultOwned = ph.GetVaultOwned(Definition.id);
        if (vaultOwned < 0) vaultOwned = 0;
        if (vaultOwned < Definition.vaultSizes.Length) return Definition.vaultSizes[vaultOwned];
        return 0;
    }

    public void DecreaseVaultMoney(double amount)
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || amount < 0) return;
        ph.ChangeRegionVaultMoney(Definition.id, -1 * amount);
    }

    public void AddMoney(double amount)
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || amount < 0) return;
        ph.ChangeRegionMoney(Definition.id, amount);
    }

    public void DecreaseMoney(double amount)
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;
        if (ph == null || amount < 0) return;
        ph.ChangeRegionMoney(Definition.id, -1 * amount);
    }

    public double GetMoney()
    {
        pELiNhaLLitSIJa ph = pELiNhaLLitSIJa.Instance;

        if (ph == null || Definition == null) return 0d;
        return ph.GetRegionMoney(Definition.id);
    }

    public int Stars
    {
        get
        {
            int total = 0;

            foreach (ImprovementState improvement in Improvements)
            {
                total += improvement.Stars;
            }

            return total;
        }
    }

    public void Unlock()
    {
        IsUnlocked = true;
    }

    public void SetUnlocked(bool unlocked)
    {
        IsUnlocked = unlocked;
    }

    public bool SetImprovementLevel(int improvementIndex, int level, bool resetProgress = true)
    {
        if (improvementIndex < 0 || improvementIndex >= Improvements.Count)
            return false;

        Improvements[improvementIndex].SetLevel(level, resetProgress);
        return true;
    }

    public int GetImprovementLevel(int improvementId)
    {
        if (Improvements == null) return 0;
        for (int i = 0; i < Improvements.Count; i++)
        {
            ImprovementState improvement = Improvements[i];
            if (improvement == null || improvement.Definition == null) continue;
            if (improvement.Definition.id == improvementId)
            {
                return improvement.Level;
            }
        }
        return 0;
    }

    public bool CanBuyImprovement(int improvementIndex, double money)
    {
        if (!IsUnlocked)
            return false;

        if (improvementIndex < 0 || improvementIndex >= Improvements.Count)
            return false;

        if (Definition.requirePreviousImprovementLevel25ForPurchase && improvementIndex > 0)
        {
            ImprovementState previous = Improvements[improvementIndex - 1];

            if (previous.Level < 25)
                return false;
        }

        return Improvements[improvementIndex].CanBuy(money);
    }

    public bool TryBuyImprovement(int improvementIndex, ref double money)
    {
        if (!CanBuyImprovement(improvementIndex, money))
            return false;

        return Improvements[improvementIndex].TryBuy(ref money);
    }

    public double Tick(double deltaSeconds)
    {
        if (!IsUnlocked)
            return 0;

        double earned = 0;

        foreach (ImprovementState improvement in Improvements)
        {
            earned += improvement.Tick(deltaSeconds);
        }

        return earned;
    }
}
