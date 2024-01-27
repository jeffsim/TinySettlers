using System;
using UnityEngine;

[Serializable]
public class CraftingSpotData : ItemSpotData
{
    public override string ToString() => "CraftingSpot " + InstanceId;

    public CraftingSpotData(BuildingData buildingData, int index) : base(buildingData)
    {
        Debug.Assert(buildingData.Defn.CraftingSpots.Count > index, "building " + buildingData.DefnId + " missing CraftingSpotData " + index);
        var loc = buildingData.Defn.CraftingSpots[index];
        LocalLoc = new Vector3(loc.x, loc.y, 0);
    }
}