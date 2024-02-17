using System;
using System.Collections.Generic;
using UnityEngine;

public interface IOccupantMgrProvider
{
    OccupantMgrComponent OccupantMgr { get; }
}

[Serializable]
public class OccupantMgrComponent : BaseData
{
    public override string ToString() => "OccupantMgr " + InstanceId;

    public List<IOccupantProvider> Occupants = new();
    public int MaxOccupants;
    public BuildingData Building;
    public int NumOccupants => Occupants.Count;
    public bool HasRoom => NumOccupants < MaxOccupants;
    public bool IsFull => NumOccupants == MaxOccupants;
    public bool IsOccupant(IOccupantProvider occupant) => Occupants.Contains(occupant);

    public OccupantMgrComponent(BuildingData buildingData)
    {
        Building = buildingData;
        MaxOccupants = buildingData.Defn.MaxWorkersLivingHere;
    }

    public void AddOccupant(IOccupantProvider occupant)
    {
        Debug.Assert(!Occupants.Contains(occupant), "Adding already added occupant " + occupant);
        Debug.Assert(!IsFull, "Adding to full " + this);
        Occupants.Add(occupant);
        occupant.Occupant.OnBecameOccupant(this);
    }

    public void RemoveOccupant(IOccupantProvider occupant)
    {
        Debug.Assert(Occupants.Contains(occupant), "Removing non-occupant " + occupant);
        Occupants.Remove(occupant);
    }

    internal void EvictAllOccupants()
    {
        foreach (var worker in Occupants)
            worker.Occupant.OnNolongerAnOccupant();
        Occupants.Clear();
    }
}