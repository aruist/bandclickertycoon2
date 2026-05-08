using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Idle Game/World Definition")]
public class WorldDefinition : ScriptableObject
{
    [Header("Starting State")]
    public double startingMoney = 10;

    [Header("Regions")]
    public List<RegionDefinition> regions = new();
}