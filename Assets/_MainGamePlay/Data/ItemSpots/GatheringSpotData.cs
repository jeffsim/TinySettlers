using System;
using UnityEngine;

[Serializable]
public class GatheringSpotData : BaseData, ILocationProvider, IReservationProvider, IItemSpotInBuilding
{
    public override string ToString() => $"Gathering {InstanceId}: {ItemContainer} {Reservation}";
    [SerializeField] public BuildingData Building { get; set; }

    public string ItemGrownInSpotDefnId;
    public float PercentGrown;

    [SerializeField] public LocationComponent Location { get; set; } = new();
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    [SerializeField] public ItemContainerComponent ItemContainer { get; set; } = new();
    public Vector3 LocOffset;

    public GatheringSpotData(BuildingData building, int index)
    {
        Building = building;
        Debug.Assert(building.Defn.GatheringSpots.Count > index, "building " + building.DefnId + " missing GatheringSpotData " + index);
        var loc = building.Defn.GatheringSpots[index];
        LocOffset = new(loc.x, Settings.Current.ItemSpotsY, loc.y);

        // hack
        if (building.Defn.ResourcesCanBeGatheredFromHere)
            ItemGrownInSpotDefnId = building.Defn.ResourcesThatCanBeGatheredFromHere[0].Id;
    }

    public void UpdateWorldLoc()
    {
        Location.SetWorldLoc(Building.Location.WorldLoc + LocOffset);
    }

    public void Update()
    {
        // If there's already an item in the spot then we can't further grow until it's reaped
        if (ItemContainer.Item != null) return;
        Debug.Assert(ItemGrownInSpotDefnId != null, "ItemGrownInSpotDefnId is null");

        // Grow the item in spot; when fully grown, create an item so that it needs to be reaped
        var itemDefn = GameDefns.Instance.ItemDefns[ItemGrownInSpotDefnId];
        PercentGrown += GameTime.deltaTime / itemDefn.SecondsToGrow;
        if (PercentGrown >= 1)
        {
            PercentGrown = 0;
            ItemContainer.SetItem(new ItemData() { DefnId = itemDefn.Id });
        }
    }

    internal void OnBuildingDestroyed()
    {
        if (!ItemContainer.IsEmpty)
            Building.Town.AddItemToGround(ItemContainer.ClearItem(), Location);
    }
}