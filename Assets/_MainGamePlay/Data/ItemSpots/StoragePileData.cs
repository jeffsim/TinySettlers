using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StoragePileData : BaseData
{
    public BuildingData Building;
    [SerializeField] StorageAreaData Area;
    public int IndexInStorageArea;

    public List<StorageSpotData> StorageSpots = new();
    public bool HasAvailableSpot => NumAvailableSpots > 0;
    public int NumStorageSpots => StorageSpots.Count;
    public int NumAvailableSpots => StorageSpots.Count(spot => spot.IsEmptyAndAvailable);
    public int NumReservedSpots => StorageSpots.Count(spot => spot.Reservation.IsReserved);
    public int NumItemsInPile => StorageSpots.Count(spot => spot.HasItem);

    public LocationComponent Location;

    public virtual void UpdateWorldLoc() => Location.WorldLoc = Area.Location.WorldLoc + Location.LocalLoc;

    public StoragePileData(StorageAreaData area, StorageAreaDefn areaDefn, Vector2 localLoc, int pileIndex)
    {
        Area = area;
        IndexInStorageArea = pileIndex;
        Building = area.Building;
        Location = new LocationComponent(area.Location, localLoc.x, localLoc.y);

        for (int i = 0, y1 = 0; y1 < areaDefn.StoragePileSize.y; y1++)
            for (int x1 = 0; x1 < areaDefn.StoragePileSize.x; x1++, i++)
                StorageSpots.Add(new(this, i));
    }

    public int NumItemsInStorage(ItemDefn itemDefn = null)
    {
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

    internal void OnBuildingDestroyed()
    {
        foreach (var spot in StorageSpots)
            spot.OnBuildingDestroyed();
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
}