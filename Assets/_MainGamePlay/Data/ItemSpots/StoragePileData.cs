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

    public Location Location = new();
    public List<StorageSpotData> StorageSpots = new();
    public bool HasAvailableSpot => NumAvailableSpots > 0;
    public int NumStorageSpots => StorageSpots.Count;
    public int NumAvailableSpots => StorageSpots.Count(spot => spot.ItemContainer.IsEmpty && !spot.Reservable.IsReserved);
    public int NumReservedSpots => StorageSpots.Count(spot => spot.Reservable.IsReserved);
    public int NumItemsInPile => StorageSpots.Count(spot => spot.ItemContainer.HasItem);

    public Vector3 PileLocOffset;
    [SerializeReference] StorageAreaData Area;

    public StoragePileData(StorageAreaData area, StorageAreaDefn areaDefn, Vector3 pileLocOffset, int pileIndex)
    {
        Area = area;
        PileLocOffset = pileLocOffset;
        IndexInStorageArea = pileIndex;
        Building = area.Building;

        var numSpots = areaDefn.StoragePileSize.x * areaDefn.StoragePileSize.y;
        for (int i = 0; i < numSpots; i++)
            StorageSpots.Add(new(this, i));
    }

    public void UpdateWorldLoc()
    {
        Location.SetWorldLoc(Area.Location.WorldLoc + PileLocOffset);
        foreach (var spot in StorageSpots)
            spot.Location.SetWorldLoc(Area.Location.WorldLoc + PileLocOffset);
    }

    public int NumItemsInStorage() => StorageSpots.Count(spot => spot.ItemContainer.HasItem);
    public int NumItemsOfTypeInStorage(ItemDefn itemDefn) => StorageSpots.Count(spot => spot.ItemContainer.ContainsItem(itemDefn));
    public int NumUnreservedItemsInStorage() => StorageSpots.Count(spot => !spot.Reservable.IsReserved && !spot.ItemContainer.IsEmpty);
    public int NumUnreservedItemsOfTypeInStorage(ItemDefn itemDefn) => StorageSpots.Count(spot => !spot.Reservable.IsReserved && spot.ItemContainer.ContainsItem(itemDefn));

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
                if (spot.Reservable.IsReserved)
                    spot.Reservable.ReservedBy.AI.CurrentTask.Abandon();
                spot.ItemContainer.ClearItem();
            }
    }
}