using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public interface IOccupantMgrComponent
{
    OccupantMgrComponent OccupantMgr { get; }
}

[Serializable]
public class OccupantMgrComponent : BaseData
{
    public override string ToString() => "OccupantMgr " + InstanceId;

    public List<WorkerData> Occupants = new();
    public int MaxOccupants;
    public BuildingData Building;
    public int NumOccupants => Occupants.Count;
    public bool IsFull => NumOccupants == MaxOccupants;
    public bool IsOccupant(WorkerData worker) => Occupants.Contains(worker);

    public OccupantMgrComponent(BuildingData buildingData)
    {
        Building = buildingData;
        MaxOccupants = buildingData.Defn.MaxWorkersLivingHere;
    }

    public void AddOccupant(WorkerData worker)
    {
        Debug.Assert(!Occupants.Contains(worker), "Adding already added occupant " + worker);
        Debug.Assert(!IsFull, "Adding to full " + this);
        Occupants.Add(worker);
    }

    public void RemoveOccupant(WorkerData worker)
    {
        Debug.Assert(Occupants.Contains(worker), "Removing non-occupant " + worker);
        Occupants.Remove(worker);
    }
}