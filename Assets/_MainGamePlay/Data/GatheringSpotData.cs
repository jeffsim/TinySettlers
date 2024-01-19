using System;
using UnityEngine;

[Serializable]
public class GatheringSpotData : BaseData
{
    public WorkerData ReservedBy;
    public bool IsReserved => ReservedBy != null;

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
    [SerializeField] public BuildingData Building;

    public GatheringSpotData(BuildingData buildingData, int index)
    {
        Debug.Assert(buildingData.Defn.GatheringSpots.Count > index, "building " + buildingData.DefnId + " missing GatheringSpotData " + index);
        Building = buildingData;
        var loc = buildingData.Defn.GatheringSpots[index];
        LocalLoc = new Vector3(loc.x, loc.y, 0);
    }

    public void Unreserve()
    {
        Debug.Assert(IsReserved, "Unreserving already unreserved GatheringSpot");
        ReservedBy = null;
    }

    public void ReserveBy(WorkerData worker)
    {
        Debug.Assert(!IsReserved, "Reserving already reserved GatheringSpot");
        Debug.Assert(worker != null, "Null reserver");

        ReservedBy = worker;
    }

    internal void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Building.WorldLoc;
    }
}