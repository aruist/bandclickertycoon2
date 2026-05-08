using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Idle Game/Region Definition")]
public class RegionDefinition : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string displayName;

    [Header("Unlock")]
    [Tooltip("How many stars are required from the previous region to unlock this region.")]
    public int starsRequiredFromPreviousRegion = 0;

    [Header("Balancing")]
    [Tooltip("Target play time for this region until all improvements naturally reach level 25.")]
    public double targetPlayMinutes = 60;

    [Tooltip("First improvement unlock price.")]
    public double firstImprovementPrice = 10;

    [Tooltip("Which previous improvement level is used to price the next improvement unlock.")]
    public int nextImprovementReferenceLevel = 24;

    [Tooltip("How expensive the next improvement is compared to the previous improvement's level 25 cost.")]
    public double nextImprovementUnlockMultiplier = 1.15;

    [Tooltip("Optional hard rule: player must level previous improvement to 25 before buying the next.")]
    public bool requirePreviousImprovementLevel25ForPurchase = false;

    [Header("Improvements")]
    public List<ImprovementDefinition> improvements = new();

    [Header("Region resource name. Contains 3d models")]
    public string resourceName;

    public double[] vaultSizes;

    [ContextMenu("Auto Balance Region")]
    public void AutoBalanceRegion()
    {
        #if UNITY_EDITOR
        Undo.RecordObject(this, "Auto Balance Region");
        #endif

        if (improvements == null || improvements.Count == 0)
        {
            Debug.LogWarning($"{name}: Region has no improvements.");
            return;
        }

        double targetSeconds = targetPlayMinutes * 60.0;
        double targetSecondsPerImprovement = targetSeconds / improvements.Count;

        for (int i = 0; i < improvements.Count; i++)
        {
            ImprovementDefinition improvement = improvements[i];

            if (improvement == null)
                continue;

            if (i == 0)
            {
                improvement.startingPrice = firstImprovementPrice;
            }
            else
            {
                ImprovementDefinition previous = improvements[i - 1];

                int referenceLevel = Mathf.Clamp(nextImprovementReferenceLevel, 1, 199);

                double previousCostToLevel25 = previous.GetTotalCostToReachLevel(25);
                double previousReferencePrice = previous.GetUpgradeCost(referenceLevel);
                improvement.startingPrice =
                    ImprovementDefinition.NiceNumber(previousReferencePrice * nextImprovementUnlockMultiplier);
                // improvement.startingPrice =
                //     ImprovementDefinition.NiceNumber(previousCostToLevel25 * nextImprovementUnlockMultiplier);
            }

            improvement.AutoBalanceProfitForTargetTime(targetSecondsPerImprovement);
        }

        Debug.Log($"{name}: Region auto-balanced for {targetPlayMinutes} minutes.");
        #if UNITY_EDITOR
        for (int i=0; i < improvements.Count; i++)
        {
            EditorUtility.SetDirty(improvements[i]);
        }
        EditorUtility.SetDirty(this);
        PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        #endif

    }

    public int GetMaxStars()
    {
        return improvements.Count * 5;
    }
}