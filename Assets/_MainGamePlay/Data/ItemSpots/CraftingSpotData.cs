using System;
using UnityEngine;

[Serializable]
public class CraftingSpotData : BaseData, ILocation, IReservable, IContainerInBuilding
{
    public override string ToString() => $"Crafting {InstanceId}: {Container} {Reservable}";

    [SerializeField] public BuildingData Building { get; set; }

    [SerializeField] public Location Location { get; set; } = new();
    [SerializeField] public Reservable Reservable { get; set; }
    [SerializeField] public Container Container { get; set; } = new();
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
        foreach (var item in Container.Items)
            Building.Town.AddItemToGround(item, Location);
        Container.ClearItems();
    }
}