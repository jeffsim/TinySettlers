using System;
using UnityEngine;

[Serializable]
public class CraftingSpotData : BaseData, ILocationProvider, IReservationProvider, IMultipleItemSpotInBuilding
{
    public override string ToString() => $"Crafting {InstanceId}: {ItemsContainer} {Reservation}";

    [SerializeField] public BuildingData Building { get; set; }

    [SerializeField] public LocationComponent Location { get; set; } = new();
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    [SerializeField] public MultipleItemContainerComponent ItemsContainer { get; set; } = new();
    public Vector2 LocOffset;

    public CraftingSpotData(BuildingData building, int index)
    {
        Debug.Assert(building.Defn.CraftingSpots.Count > index, "building " + building.DefnId + " missing CraftingSpotData " + index);
        Building = building;
        LocOffset = new(building.Defn.CraftingSpots[index].x, building.Defn.CraftingSpots[index].y);
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