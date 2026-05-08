using OneP.InfinityScrollView;
using UnityEngine;

public class IdleGameController : MonoBehaviour
{
    [SerializeField] private WorldDefinition worldDefinition;
    [SerializeField] private InfinityScrollView scrollView;

    [Header("Debug only")]
    [SerializeField] private double currentMoney;
    [SerializeField] private double[] improvement1Price = new double[10];

    public WorldState World { get; private set; }

    public double Money => World != null ? World.Money : 0;

    private void Awake()
    {
        World = new WorldState(worldDefinition);
    }

    private void Update()
    {
        currentMoney = Money;
        for (int i=0; i < 10; i++)
        {
            improvement1Price[i] = GetImprovementPrice(0,i);
        }
        World.Tick(Time.deltaTime);
    }

    [ContextMenu("UpgradeItem0")]
    public void UpgradeItem0()
    {
        TryBuyImprovement(0,0);
    }

    public bool TryBuyImprovement(int regionIndex, int improvementIndex)
    {
        return World.TryBuyImprovement(regionIndex, improvementIndex);
    }

    public bool TryUnlockRegion(int regionIndex)
    {
        return World.TryUnlockRegion(regionIndex);
    }

    public int GetRegionStars(int regionIndex)
    {
        if (regionIndex < 0 || regionIndex >= World.Regions.Count)
            return 0;

        return World.Regions[regionIndex].Stars;
    }

    public bool IsRegionUnlocked(int regionIndex)
    {
        if (regionIndex < 0 || regionIndex >= World.Regions.Count)
            return false;

        return World.Regions[regionIndex].IsUnlocked;
    }

    public double GetImprovementPrice(int regionIndex, int improvementIndex)
    {
        if (World == null)
            return 0;

        if (regionIndex < 0 || regionIndex >= World.Regions.Count)
            return 0;

        RegionState region = World.Regions[regionIndex];

        if (improvementIndex < 0 || improvementIndex >= region.Improvements.Count)
            return 0;

        ImprovementState improvement = region.Improvements[improvementIndex];

        return improvement.GetNextUpgradeCost();
    }

}