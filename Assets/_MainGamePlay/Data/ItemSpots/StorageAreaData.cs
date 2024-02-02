using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StorageAreaData : BaseData
{
    public override string ToString() => "{" + string.Join(", ", StoragePiles.Select(spot => spot)) + "}";

    public LocationComponent Location;
    public List<StoragePileData> StoragePiles;

    public BuildingData Building;

    public int NumStorageSpots => StoragePiles.Sum(spot => spot.NumStorageSpots);
    public int NumAvailableSpots => StoragePiles.Sum(spot => spot.NumAvailableSpots);
    public int NumReservedSpots => StoragePiles.Sum(spot => spot.NumReservedSpots);
    public bool HasAvailableSpot => StoragePiles.Any(spot => spot.HasAvailableSpot);

    public StorageAreaData(BuildingData buildingData, StorageAreaDefn storageAreaDefn)
    {
        Building = buildingData;

        Location = new(Building.Location, storageAreaDefn.Location.x, storageAreaDefn.Location.y);
        StoragePiles = new();
        var width = storageAreaDefn.StorageAreaSize.x;
        var height = storageAreaDefn.StorageAreaSize.y;
        for (int i = 0, y = 0; y < height; y++)
            for (int x = 0; x < width; x++, i++)
            {
                Vector2 pileLocation = new((x - (width - 1) / 2f) * 1.1f, (y - (height - 1) / 2f) * 1.1f);
                StoragePiles.Add(new(this, storageAreaDefn, pileLocation, i));
            }
    }

    public void UpdateWorldLoc()
    {
        Location.UpdateWorldLoc();
        foreach (var pile in StoragePiles)
            pile.UpdateWorldLoc();
    }

    public int NumItemsInStorage(ItemDefn itemDefn = null) => StoragePiles.Sum(spot => spot.NumItemsInStorage(itemDefn));

    public int NumUnreservedItemsInStorage(ItemDefn itemDefn = null) => StoragePiles.Sum(spot => spot.NumUnreservedItemsInStorage(itemDefn));

    internal void Debug_RemoveAllItemsFromStorage()
    {
        foreach (var pile in StoragePiles)
            pile.Debug_RemoveAllItemsFromStorage();
    }

    internal void OnBuildingDestroyed()
    {
        foreach (var pile in StoragePiles)
            pile.OnBuildingDestroyed();
    }
}