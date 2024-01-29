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

    public ItemData ItemInHand;
    public StorageSpotData StorageSpotReservedForItemInHand;

    public NeedData OriginalPickupItemNeed;

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

    internal void OnNeedBeingMetCancelled()
    {
        throw new NotImplementedException();
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public void OnBuildingDestroyed(BuildingData building)
    {
        CurrentTask?.OnBuildingDestroyed(building);

        // If we are assigned to the destroyed building, then assign ourselves to the Camp instead
        if (AssignedBuilding == building)
            AssignToBuilding(Town.Camp);
    }

    public void OnBuildingMoved(BuildingData building, Vector2 previousWorldLoc)
    {
        CurrentTask?.OnBuildingMoved(building, previousWorldLoc);
    }
    public void OnBuildingPauseToggled(BuildingData building)
    {
        CurrentTask?.OnBuildingPauseToggled(building);
    }

    public void Destroy()
    {
        // Unassign from building; this will abandon current Task
        if (AssignedBuilding != null)
            AssignToBuilding(null);
    }

    internal bool HasPathToBuilding(BuildingData buildingWithNeed)
    {
        // TODO
        return true;
    }
    internal bool HasPathToItemOnGround(ItemData itemOnGround)
    {
        // TODO
        return true;
    }

    internal float GetMovementSpeed()
    {
        // todo: can be modified via e.g. research, town upgrades, ...
        var distanceMovedPerSecond = 5f;
        if (ItemInHand != null)
            distanceMovedPerSecond *= ItemInHand.Defn.CarryingSpeedModifier;
        return distanceMovedPerSecond;
    }

    internal void AddItemToHands(ItemData item)
    {
        Debug.Assert(item != null, "null item");
        Debug.Assert(ItemInHand == null, "Already have ItemInHand (" + ItemInHand + ")");
        ItemInHand = item;
    }

    internal void DropItemInHandInReservedStorageSpot()
    {
        Debug.Assert(StorageSpotReservedForItemInHand != null, "No StorageSpotReservedForItemInHand");
        Debug.Assert(StorageSpotReservedForItemInHand.Building != null, "No ItemInHand");
        Debug.Assert(!StorageSpotReservedForItemInHand.Building.IsDestroyed, "Building destroyed");
        Debug.Assert(ItemInHand != null, "No ItemInHand");

        // This intentionally does not unreserve the reserved storagespot; caller is responsible for doing that
        StorageSpotReservedForItemInHand.Building.AddItemToItemSpot(ItemInHand, StorageSpotReservedForItemInHand);
        ItemInHand = null;
        UnreserveStorageSpotReservedForItemInHand();
    }

    internal bool CanGatherResource(ItemDefn neededItem)
    {
        // TODO: Rather than tie to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
        // worker is assigned to building and/or by worker's defn
        return (AssignedBuilding.Defn.CanGatherResources && AssignedBuilding.Defn.GatherableResources.Contains(neededItem))
                 //  || AssignedBuilding.Defn.IsPrimaryStorage
                 ;
    }

    internal bool CanCleanupStorage()
    {
        // TODO: Rather than tie to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
        // worker is assigned to building and/or by worker's defn
        return AssignedBuilding.Defn.WorkersCanFerryItems;
    }

    internal bool CanPickupAbandonedItems()
    {
        // TODO: Rather than tie to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
        // worker is assigned to building and/or by worker's defn
        return AssignedBuilding.Defn.WorkersCanFerryItems;
    }

    internal bool CanGoGetItemsBuildingsWant()
    {
        // TODO: Rather than tie to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
        // worker is assigned to building and/or by worker's defn
        return AssignedBuilding.Defn.WorkersCanFerryItems;
    }

    internal bool CanCraftItems()
    {
        // TODO: Rather than tie to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
        // worker is assigned to building and/or by worker's defn
        return AssignedBuilding.Defn.CanCraft;
    }

    internal void UnreserveStorageSpotReservedForItemInHand()
    {
        Debug.Assert(StorageSpotReservedForItemInHand != null, "No StorageSpotReservedForItemInHand");
        Debug.Assert(StorageSpotReservedForItemInHand.ReservedBy == this, "StorageSpotReservedForItemInHand.ReservedBy != this");

        StorageSpotReservedForItemInHand.Unreserve();
        StorageSpotReservedForItemInHand = null;
    }
}