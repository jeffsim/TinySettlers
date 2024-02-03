using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StoragePileData : BaseData
{
    public override string ToString() => "{" + string.Join(", ", StorageSpots.Select(spot => spot)) + "}";

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

    public virtual void UpdateWorldLoc()
    {
        Location.WorldLoc = Area.Location.WorldLoc + Location.LocalLoc;
        foreach (var spot in StorageSpots)
            spot.UpdateWorldLoc();
    }

    public StoragePileData(StorageAreaData area, StorageAreaDefn areaDefn, Vector2 localLoc, int pileIndex)
    {
        Area = area;
        IndexInStorageArea = pileIndex;
        Building = area.Building;
        Location = new LocationComponent(area.Location, localLoc);

        var numSpots = areaDefn.StoragePileSize.x * areaDefn.StoragePileSize.y;
        for (int i = 0; i < numSpots; i++)
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
        if (itemDefn == null)
            return StorageSpots.Count(spot => !spot.Reservation.IsReserved && !spot.ItemContainer.IsEmpty);
        return StorageSpots.Count(spot => !spot.Reservation.IsReserved && spot.ItemContainer.ContainsItem(itemDefn));
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