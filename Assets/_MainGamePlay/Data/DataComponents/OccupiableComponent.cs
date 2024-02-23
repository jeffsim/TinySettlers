using System;
using System.Collections.Generic;
using UnityEngine;

public interface IOccupiable
{
    OccupiableComponent Occupiable { get; }
}

[Serializable]
public class OccupiableComponent : BaseData
{
    public override string ToString() => "OccupantMgr " + InstanceId;

    public List<IOccupantProvider> Occupants = new();
    public int MaxOccupants;
    public int NumOccupants => Occupants.Count;
    public bool HasRoom => NumOccupants < MaxOccupants;
    public bool IsFull => NumOccupants == MaxOccupants;
    public bool IsOccupant(IOccupantProvider occupant) => Occupants.Contains(occupant);

    public OccupiableComponent(OccupiableDefn defn)
    {
        MaxOccupants = defn.MaxWorkersLivingHere;
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
            worker.Occupant.OnEvicted();
        Occupants.Clear();
    }
}