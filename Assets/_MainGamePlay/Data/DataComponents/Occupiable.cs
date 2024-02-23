using System;
using System.Collections.Generic;
using UnityEngine;

public interface IOccupiable
{
    Occupiable Occupiable { get; }
}

[Serializable]
public class Occupiable : BaseData
{
    public override string ToString() => $"Occupiable: {NumOccupants}/{MaxOccupants}";

    public List<IOccupier> Occupants = new();
    public int MaxOccupants;
    public int NumOccupants => Occupants.Count;
    public bool HasRoom => NumOccupants < MaxOccupants;
    public bool IsFull => NumOccupants == MaxOccupants;
    public bool IsOccupant(IOccupier occupant) => Occupants.Contains(occupant);

    [SerializeField] IOccupiable Owner;

    public Occupiable(OccupiableDefn defn, IOccupiable owner)
    {
        Owner = owner;
        MaxOccupants = defn.MaxWorkersLivingHere;
    }

    public void AddOccupant(IOccupier occupant)
    {
        Debug.Assert(!Occupants.Contains(occupant), "Adding already added occupant " + occupant);
        Debug.Assert(!IsFull, "Adding to full " + this);
        Occupants.Add(occupant);
        occupant.Occupier.OnBecameOccupant(this);
    }

    public void RemoveOccupant(IOccupier occupant)
    {
        Debug.Assert(Occupants.Contains(occupant), "Removing non-occupant " + occupant);
        Occupants.Remove(occupant);
    }

    internal void EvictAllOccupants()
    {
        foreach (var worker in Occupants)
            worker.Occupier.OnEvicted();
        Occupants.Clear();
    }
}