using System;
using UnityEngine;

public delegate void OnAssignedToBuildingEvent();

[Serializable]
public class WorkerData : BaseData
{
    public override string ToString() => AssignedBuilding.Defn.AssignedWorkerFriendlyName + " (" + InstanceId + ")";// + "-" + worker.Data.UniqueId;

    // Position within the current Map
    public Vector3 WorldLoc;

    public WorkerTask CurrentTask;
    public BuildingData AssignedBuilding;

    public bool IsIdle => CurrentTask.Type == TaskType.Idle;

    public WorkerTask_Idle IdleTask;

    // The Town which this Worker is in
    public TownData Town;

    [NonSerialized] public OnAssignedToBuildingEvent OnAssignedToBuilding;

    public WorkerData(BuildingData buildingToStartIn)
    {
        WorldLoc = Utilities.LocationWithinDistance(new Vector2(buildingToStartIn.WorldLoc.x, buildingToStartIn.WorldLoc.y), 1);

        Town = buildingToStartIn.Town;

        if (buildingToStartIn != null)
            AssignToBuilding(buildingToStartIn);

        CurrentTask = IdleTask = WorkerTask_Idle.Create(this);
        CurrentTask.Start(); // start out idling
    }

    internal void AssignToBuilding(BuildingData building)
    {
        Debug.Assert(building != AssignedBuilding, "Reassigning to same building");
        Debug.Assert(building != null, "Assigning to null building");

        CurrentTask?.Abandon();
        AssignedBuilding = building;
        OnAssignedToBuilding?.Invoke();
    }

    internal void OnTaskCompleted(bool wasAbandoned)
    {
        CurrentTask = IdleTask;
        CurrentTask.Start();
    }

    public void Update()
    {
        CurrentTask.Update();
    }

    internal float DistanceToBuilding(BuildingData target)
    {
        return Vector2.Distance(WorldLoc, target.WorldLoc);
    }

    internal void OnNeedBeingMetCanspoted()
    {
        throw new NotImplementedException();
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public void OnBuildingDestroyed(BuildingData building)
    {
        // If we are assigned to the destroyed building, then assign ourselves to the Camp instead
        if (AssignedBuilding == building)
            AssignToBuilding(Town.Camp);

        CurrentTask?.OnBuildingDestroyed(building);
    }

    public void OnBuildingMoved(BuildingData building, Vector2 previousWorldLoc)
    {
        CurrentTask?.OnBuildingMoved(building, previousWorldLoc);
    }

    public void Destroy()
    {
        // Unassign from building; this will abandon current Task
        if (AssignedBuilding != null)
            AssignToBuilding(null);
    }

    internal bool HasPathToRoom(BuildingData buildingWithNeed)
    {
        // TODO
        return true;
    }
    internal bool HasPathToItemOnGround(ItemData itemOnGround)
    {
        // TODO
        return true;
    }
}