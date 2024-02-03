using System;
using UnityEngine;

[Serializable]
public class CraftingSpotData : BaseData, ILocationProvider, IReservationProvider
{
    public override string ToString() => ItemsContainer.ToString();

    public BuildingData Building;
    
    [SerializeField] public LocationComponent Location { get; set; }
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    public MultipleItemContainerComponent ItemsContainer = new();

    public CraftingSpotData(BuildingData building, int index)
    {
        Debug.Assert(building.Defn.CraftingSpots.Count > index, "building " + building.DefnId + " missing CraftingSpotData " + index);
        Building = building;
        Location = new(building.Location, building.Defn.CraftingSpots[index].x, building.Defn.CraftingSpots[index].y);
    }

    internal void OnBuildingDestroyed()
    {
        // drop crafting resources onto the ground
        foreach (var item in ItemsContainer.Items)
            Building.Town.AddItemToGround(item, Location);
        ItemsContainer.ClearItems();
    }
}