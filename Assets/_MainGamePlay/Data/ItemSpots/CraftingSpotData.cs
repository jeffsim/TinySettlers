using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CraftingSpotData : ReservableData
{
    public override string ToString() => CraftingResourcesInSpot.Count == 0 ? "empty" : "{" + string.Join(", ", CraftingResourcesInSpot.Select(item => item)) + "}";

    public BuildingData Building;
    public List<ItemData> CraftingResourcesInSpot = new();

    [SerializeField] Vector2 _localLoc;
    public Vector3 LocalLoc // relative to our Building
    {
        get => _localLoc;
        set
        {
            _localLoc = value;
            UpdateWorldLoc();
        }
    }

    public Vector3 WorldLoc; // relative to the world

    public CraftingSpotData(BuildingData buildingData, int index)
    {
        Debug.Assert(buildingData.Defn.CraftingSpots.Count > index, "building " + buildingData.DefnId + " missing CraftingSpotData " + index);
        Building = buildingData;
        var loc = buildingData.Defn.CraftingSpots[index];
        LocalLoc = new Vector3(loc.x, loc.y, 0);
    }

    public virtual void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Building.WorldLoc;
    }

    public void AddItem(ItemData item)
    {
        CraftingResourcesInSpot.Add(item);
    }

    public void ConsumeAllCraftingResources()
    {
        CraftingResourcesInSpot.Clear();
    }

    internal void OnBuildingDestroyed()
    {
        // drop crafting resources onto the ground
        foreach (var item in CraftingResourcesInSpot)
            Building.Town.AddItemToGround(item, WorldLoc);
        CraftingResourcesInSpot.Clear();
    }
}