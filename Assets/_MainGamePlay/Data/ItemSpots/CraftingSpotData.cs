using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class CraftingSpotData : BaseData
{
    public override string ToString() => CraftingResourcesInSpot.Count == 0 ? "empty" : "{" + string.Join(", ", CraftingResourcesInSpot.Select(item => item)) + "}";

    public BuildingData Building;
    public List<ItemData> CraftingResourcesInSpot = new();

    public LocationComponent Location;
    public ReservationComponent Reservation = new();
    public ItemContainerComponent ItemContainer = new();

    public CraftingSpotData(BuildingData building, int index)
    {
        Debug.Assert(building.Defn.CraftingSpots.Count > index, "building " + building.DefnId + " missing CraftingSpotData " + index);
        Building = building;
        Location = new(building.Location, building.Defn.CraftingSpots[index]);
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
            Building.Town.AddItemToGround(item, Location);
        CraftingResourcesInSpot.Clear();
    }
}