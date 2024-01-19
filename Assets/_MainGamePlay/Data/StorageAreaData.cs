using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StorageAreaData : BaseData
{
    public List<StorageSpotData> StorageSpots;

    [SerializeField] Vector3 _localLoc;
    public Vector3 LocalLoc // relative to our Building
    {
        get => _localLoc;
        set
        {
            _localLoc = value;
            UpdateWorldLoc();
        }
    }

    public Vector3 WorldLoc;

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

        StorageSpots = new List<StorageSpotData>();
        for (int i = 0; i < 9; i++)
            StorageSpots.Add(new StorageSpotData(this, i));

        var loc = buildingData.Defn.StorageAreaLocations[index];
        LocalLoc = new Vector2(loc.x, loc.y);
    }

    public void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Building.WorldLoc;
        foreach (var spot in StorageSpots)
            spot.UpdateWorldLoc();
    }

    public int NumItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var spot in StorageSpots)
            if (!spot.IsEmpty && (itemDefn == null || spot.ItemInStorage.DefnId == itemDefn.Id)) count++;
        return count;
    }

    public int NumUnreservedItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var spot in StorageSpots)
            if (!spot.IsEmpty && (itemDefn == null ||
            (!spot.IsReserved && spot.ItemInStorage.DefnId == itemDefn.Id))) count++;
        return count;
    }

    internal void Debug_RemoveAllItemsFromStorage()
    {
        foreach (var spot in StorageSpots)
            if (!spot.IsEmpty)
                spot.RemoveItem();
    }
}