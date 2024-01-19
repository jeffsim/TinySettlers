using System;
using UnityEngine;

[Serializable]
public class DistanceToBuilding
{
    public float Distance;
    public BuildingData Building;
}

[Serializable]
public class ClosestStorageSpotWithItem : BaseData
{
    /** The resource instance to reserve */
    [SerializeReference] public StorageSpotData StorageSpot; // The spot holding the item (if any)

    /** Distance to the Item */
    public float Distance;

    public ClosestStorageSpotWithItem()
    {
    }

    // public ClosestItem(ItemData item, float distance)
    // {
    //     SetResource(item);
    //     Distance = distance;
    // }

    public ClosestStorageSpotWithItem(StorageSpotData spot, DistanceToBuilding roomDist)
    {
        StorageSpot = spot;
        Distance = roomDist.Distance;
    }

    // public ClosestItem(ClosestItem source)
    // {
    //     SetResource(source.Item);
    //     Distance = source.Distance;
    // }
}
