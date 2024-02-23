using System;
using UnityEngine;

[Serializable]
public class CraftingSpotData : BaseData, ILocation, IReservable, IMultipleItemSpotInBuilding
{
    public override string ToString() => $"Crafting {InstanceId}: {ItemsContainer} {Reservable}";

    [SerializeField] public BuildingData Building { get; set; }

    [SerializeField] public Location Location { get; set; } = new();
    [SerializeField] public Reservable Reservable { get; set; }
    [SerializeField] public MultipleContainable ItemsContainer { get; set; } = new();
    public Vector3 LocOffset;

    public CraftingSpotData(BuildingData building, int index)
    {
        Debug.Assert(building.Defn.CraftingSpots.Count > index, "building " + building.DefnId + " missing CraftingSpotData " + index);
        Building = building;
        LocOffset = new(building.Defn.CraftingSpots[index].x, Settings.Current.ItemSpotsY, building.Defn.CraftingSpots[index].y);
        Reservable = new(this);
    }

    public void UpdateWorldLoc()
    {
        Location.SetWorldLoc(Building.Location.WorldLoc + LocOffset);
    }

    internal void OnBuildingDestroyed()
    {
        // drop crafting resources onto the ground
        foreach (var item in ItemsContainer.Items)
            Building.Town.AddItemToGround(item, Location);
        ItemsContainer.ClearItems();
    }
}