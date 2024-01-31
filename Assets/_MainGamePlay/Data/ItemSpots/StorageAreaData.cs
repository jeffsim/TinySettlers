using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StorageAreaData : BaseData
{
    public override string ToString() => "{" + string.Join(", ", StorageSpots.Select(spot => spot)) + "}";

    public List<StorageSpotData> StorageSpots;
    public LocationComponent Location;

    public BuildingData Building;

    public int NumAvailableSpots
    {
        get
        {
            int count = 0;
            foreach (var spot in StorageSpots)
                if (spot.IsEmptyAndAvailable) count++;
            return count;
        }
    }
    public bool HasAvailableSpot => NumAvailableSpots > 0;

    public StorageAreaData(BuildingData buildingData, int index)
    {
        Building = buildingData;

        var loc = buildingData.Defn.StorageAreaLocations[index];
        Location = new(Building.Location, new(loc.x, loc.y));

        StorageSpots = new List<StorageSpotData>();
        var width = buildingData.Defn.StorageAreaSize.x;
        var height = buildingData.Defn.StorageAreaSize.y;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 spotLoc = new((x - (width - 1) / 2f) * 1.1f, (y - (height - 1) / 2f) * 1.1f);
                StorageSpots.Add(new StorageSpotData(this, spotLoc));
            }
    }

    public void UpdateWorldLoc()
    {
        Location.UpdateWorldLoc();
        foreach (var spot in StorageSpots)
            spot.UpdateWorldLoc();
    }

    public int NumItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var spot in StorageSpots)
            if (!spot.ItemContainer.IsEmpty && (itemDefn == null || spot.ItemContainer.Item.DefnId == itemDefn.Id)) count++;
        return count;
    }

    public int NumUnreservedItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var spot in StorageSpots)
            if (!spot.ItemContainer.IsEmpty && (itemDefn == null ||
            (!spot.Reservation.IsReserved && spot.ItemContainer.Item.DefnId == itemDefn.Id))) count++;
        return count;
    }

    internal void Debug_RemoveAllItemsFromStorage()
    {
        foreach (var spot in StorageSpots)
            if (!spot.ItemContainer.IsEmpty)
            {
                if (spot.Reservation.IsReserved)
                    spot.Reservation.ReservedBy.CurrentTask?.Abandon();
                spot.ItemContainer.ClearItem();
            }
    }

    internal void OnBuildingDestroyed()
    {
        foreach (var spot in StorageSpots)
            spot.OnBuildingDestroyed();
    }
}
