using System;
using UnityEngine;

public interface IOccupier
{
    Occupier Occupier { get; }
}

[Serializable]
public class Occupier : BaseData
{
    public override string ToString() => $"Occupier: {Home}";

    public Occupiable Home;
    public bool HasHome => Home != null;
    public bool IsHomeless => Home == null;

    [SerializeField] IOccupier Owner;

    public Occupier(IOccupier owner)
    {
        Owner = owner;
    }
    
    public void OnBecameOccupant(Occupiable home)
    {
        Debug.Assert(Home == null, "Assigning home to already-homed " + this);
        Home = home;
    }

    internal void OnEvicted()
    {
        Home = null;
    }
}