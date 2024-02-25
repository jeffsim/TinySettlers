using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class StorageAreaData : BaseData
{
    public override string ToString() => Location + "{" + string.Join(", ", StoragePiles.Select(pile => pile)) + "}";

    public Location Location = new();
    public List<StoragePileData> StoragePiles = new();

    public BuildingData Building;

    public int NumStorageSpots
    {
        get
        {
            int count = 0;
            foreach (var pile in StoragePiles)
                count += pile.NumStorageSpots;
            return count;
        }
    }
    public int NumAvailableSpots
    {
        get
        {
            int count = 0;
            foreach (var pile in StoragePiles)
                count += pile.NumAvailableSpots;
            return count;
        }
    }
    public int NumReservedSpots => StoragePiles.Sum(spot => spot.NumReservedSpots);
    public bool HasAvailableSpot => StoragePiles.Any(spot => spot.HasAvailableSpot);

    public int NumItemsInStorage 
    {
        get
        {
            int count = 0;
            foreach (var pile in StoragePiles)
                count += pile.NumItemsInStorage();
            return count;
        }
    }
    public int NumUnreservedItemsInStorage => StoragePiles.Sum(spot => spot.NumUnreservedItemsInStorage());
    public int NumItemsOfTypeInStorage(ItemDefn itemDefn) 
    {
        int count = 0;
        foreach (var pile in StoragePiles)
            count += pile.NumItemsOfTypeInStorage(itemDefn);
        return count;
    }
    public int NumUnreservedItemsOfTypeInStorage(ItemDefn itemDefn) => StoragePiles.Sum(spot => spot.NumUnreservedItemsOfTypeInStorage(itemDefn));

    public Vector3 AreaLocOffset;

    public StorageAreaData(BuildingData buildingData, StorageAreaDefn storageAreaDefn)
    {
        Building = buildingData;
        AreaLocOffset = new(storageAreaDefn.Location.x, Settings.Current.StorageAreaY, storageAreaDefn.Location.y);
        var width = storageAreaDefn.StorageAreaSize.x;
        var height = storageAreaDefn.StorageAreaSize.y;
        for (int i = 0, y = 0; y < height; y++)
            for (int x = 0; x < width; x++, i++)
            {
                Vector3 pileLocation = new((x - (width - 1) / 2f) * 1.1f, 0, (y - (height - 1) / 2f) * 1.2f);
                StoragePiles.Add(new(this, storageAreaDefn, pileLocation, i));
            }
    }

    public void UpdateWorldLoc()
    {
        Location.SetWorldLoc(Building.Location.WorldLoc + AreaLocOffset);
        foreach (var pile in StoragePiles)
            pile.UpdateWorldLoc();
    }

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