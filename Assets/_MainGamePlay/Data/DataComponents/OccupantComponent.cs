using System;
using UnityEngine;

public interface IOccupantProvider
{
    OccupantComponent Occupant { get; }
}

[Serializable]
public class OccupantComponent : BaseData
{
    public override string ToString() => "Occupant " + InstanceId;

    public OccupantMgrComponent Home;
    public bool HasHome => Home != null;
    public bool IsHomeless => Home == null;

    public OccupantComponent()
    {
    }

    public void OnBecameOccupant(OccupantMgrComponent home)
    {
        Debug.Assert(Home == null, "Assigning home to already-homed " + this);
        Home = home;
    }

    internal void OnNolongerAnOccupant()
    {
        Home = null;
    }
}