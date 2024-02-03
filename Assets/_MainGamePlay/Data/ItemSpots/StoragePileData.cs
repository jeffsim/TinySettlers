using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StoragePileData : BaseData
{
    public override string ToString() => "{" + string.Join(", ", StorageSpots.Select(spot => spot)) + "}";

    public BuildingData Building;
    public int IndexInStorageArea;

    public List<StorageSpotData> StorageSpots = new();
    public bool HasAvailableSpot => NumAvailableSpots > 0;
    public int NumStorageSpots => StorageSpots.Count;
    public int NumAvailableSpots => StorageSpots.Count(spot => spot.IsEmptyAndAvailable);
    public int NumReservedSpots => StorageSpots.Count(spot => spot.Reservation.IsReserved);
    public int NumItemsInPile => StorageSpots.Count(spot => spot.HasItem);

    public LocationComponent Location;

    public StoragePileData(StorageAreaData area, StorageAreaDefn areaDefn, Vector2 localLoc, int pileIndex)
    {
        IndexInStorageArea = pileIndex;
        Building = area.Building;
        Location = new LocationComponent(area.Location, localLoc);

        var numSpots = areaDefn.StoragePileSize.x * areaDefn.StoragePileSize.y;
        for (int i = 0; i < numSpots; i++)
            StorageSpots.Add(new(this, i));
    }

    public void UpdateWorldLoc()
    {
        Location.UpdateWorldLoc();
        foreach (var spot in StorageSpots)
            spot.Location.UpdateWorldLoc();
    }

    public int NumItemsInStorage() => StorageSpots.Count(spot => spot.ItemContainer.HasItem);
    public int NumItemsOfTypeInStorage(ItemDefn itemDefn) => StorageSpots.Count(spot => spot.ItemContainer.ContainsItem(itemDefn));
    public int NumUnreservedItemsInStorage() => StorageSpots.Count(spot => !spot.Reservation.IsReserved && !spot.ItemContainer.IsEmpty);
    public int NumUnreservedItemsOfTypeInStorage(ItemDefn itemDefn) => StorageSpots.Count(spot => !spot.Reservation.IsReserved && spot.ItemContainer.ContainsItem(itemDefn));

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