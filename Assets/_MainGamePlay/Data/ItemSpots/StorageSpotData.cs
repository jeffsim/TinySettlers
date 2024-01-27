using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : ItemSpotData
{
    [SerializeField] StorageAreaData Area;

    public StorageSpotData(StorageAreaData area, Vector2 localLoc) : base(area.Building)
    {
        Area = area;
        LocalLoc = localLoc;
    }

    public override void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Area.WorldLoc;
    }
}