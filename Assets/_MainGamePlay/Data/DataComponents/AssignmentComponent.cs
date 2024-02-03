using System;
using UnityEngine;

public interface IAssignmentProvider
{
    AssignmentComponent Assignment { get; }
}
public delegate void OnAssignedToChangedEvent();

[Serializable]
public class AssignmentComponent : BaseData
{
    public BuildingData AssignedTo;
    public bool IsAssigned => AssignedTo != null;
    [NonSerialized] public OnAssignedToChangedEvent OnAssignedToChanged;

    internal void AssignTo(BuildingData building)
    {
        Debug.Assert(building != null, "Assigning to null building");
        Debug.Assert(AssignedTo != building, "Reassigning to same building");
        AssignedTo = building;
        OnAssignedToChanged?.Invoke();
    }

    internal void UnassignFrom()
    {
        Debug.Assert(AssignedTo != null, "Unassigning from null building");
        AssignedTo = null;
        OnAssignedToChanged?.Invoke();
    }
}