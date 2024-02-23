using System;
using UnityEngine;

public interface IAssignable
{
    Assignable Assignable { get; }
    public void OnAssignedToChanged();
}

[Serializable]
public class Assignable : BaseData
{
    public override string ToString() => $"Assignable: {AssignedTo}";

    public BuildingData AssignedTo;
    public bool IsAssigned => AssignedTo != null;

    [NonSerialized] public Action OnAssignedToChanged;

    [SerializeField] IAssignable Owner;

    public Assignable(IAssignable owner)
    {
        Owner = owner;
    }

    internal void AssignTo(BuildingData building)
    {
        Debug.Assert(building != null, "Assigning to null building");
        Debug.Assert(AssignedTo != building, "Reassigning to same building");
        AssignedTo = building;
        OnAssignedToChanged?.Invoke();
        Owner.OnAssignedToChanged();
    }

    internal void UnassignFrom()
    {
        Debug.Assert(AssignedTo != null, "Unassigning from null building");
        AssignedTo = null;
        OnAssignedToChanged?.Invoke();
        Owner.OnAssignedToChanged();
    }

    public void OnDestroyed()
    {
        if (IsAssigned)
            UnassignFrom();
    }
}