using System;
using UnityEngine;

[Serializable]
public class GatheringSpotData : ItemSpotData
{
    public override string ToString() => "GatheringSpot " + InstanceId;

    public string ItemGrownInSpotDefnId;
    public float PercentGrown;

    public GatheringSpotData(BuildingData buildingData, int index) : base(buildingData)
    {
        Debug.Assert(buildingData.Defn.GatheringSpots.Count > index, "building " + buildingData.DefnId + " missing GatheringSpotData " + index);
        var loc = buildingData.Defn.GatheringSpots[index];
        LocalLoc = new Vector3(loc.x, loc.y, 0);
        PercentGrown = 0;

        // hack
        if (buildingData.Defn.ResourcesCanBeGatheredFromHere)
            ItemGrownInSpotDefnId = buildingData.Defn.ResourcesThatCanBeGatheredFromHere[0].Id;
    }

    public void Update()
    {
        // If there's already an item in the spot then we can't further grow until it's reaped
        if (ItemInSpot != null) return;
        Debug.Assert(ItemGrownInSpotDefnId != null, "ItemGrownInSpotDefnId is null");

        // Grow the item in spot; when fully grown, create an item so that it needs to be reaped
        var itemDefn = GameDefns.Instance.ItemDefns[ItemGrownInSpotDefnId];
        PercentGrown += Time.deltaTime / itemDefn.SecondsToGrow;
        if (PercentGrown >= 1)
        {
            PercentGrown = 0;
            ItemInSpot = new ItemData() { DefnId = itemDefn.Id };
        }
    }
}