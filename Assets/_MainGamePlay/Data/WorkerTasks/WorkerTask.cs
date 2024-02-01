using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType
{
    Unset, Idle,
    DeliverItemInHandToStorageSpot,
    PickupGatherableResource,
    PickupItemInStorageSpot,
    PickupItemFromGround,
    SellItem,

    CraftGood//, SellGood
};

public enum TaskState { Unset, NotStarted, Started, Completed, Abandoned };

[Serializable]
public abstract class WorkerTask
{
    public virtual TaskType Type => TaskType.Unset;
    public TaskState TaskState = TaskState.Unset;

    public bool IsRunning => TaskState == TaskState.Started;

    public virtual bool IsWalkingToTarget => false;
    internal virtual string getDebuggerString() => $"{Type}  debugger string not implemented";

    [SerializeReference] public WorkerData Worker;

    public float timeStartedSubstate;
    public int substate;

    public LocationComponent LastMoveToTarget = new();

    [SerializeField] List<GatheringSpotData> ReservedGatheringSpots;
    [SerializeField] protected List<StorageSpotData> ReservedStorageSpots;

    public bool HasReservedStorageSpot(StorageSpotData spot) => ReservedStorageSpots.Contains(spot);
    public bool HasReservedCraftingSpot(CraftingSpotData spot) => ReservedCraftingSpots.Contains(spot);
    public bool HasReservedGatheringSpot(GatheringSpotData spot) => ReservedGatheringSpots.Contains(spot);

    public abstract string ToDebugString();

    public virtual bool IsCarryingItem(string itemId) => false;

    [SerializeField] List<StorageSpotData> ReservedCraftingResourceStorageSpots;
    [SerializeField] List<CraftingSpotData> ReservedCraftingSpots;

    [SerializeField] protected float distanceMovedPerSecond = 5;

    // The Need that this task is meeting
    public NeedData Need;

    public virtual ItemDefn GetTaskItem()
    {
        Debug.Assert(false, "GetTaskItem not implemented for task type " + Type);
        return null;
    }

    protected WorkerTask(WorkerData workerData, NeedData need)
    {
        Debug.Assert(need != null, "Need is null");
        Need = need;
        Worker = workerData;
        ReservedGatheringSpots = new List<GatheringSpotData>();
        ReservedStorageSpots = new List<StorageSpotData>();
        ReservedCraftingSpots = new List<CraftingSpotData>();
        ReservedCraftingResourceStorageSpots = new List<StorageSpotData>();
    }

    // TODO: remove this constructor
    protected WorkerTask(WorkerData workerData)
    {
        Worker = workerData;
        ReservedGatheringSpots = new List<GatheringSpotData>();
        ReservedStorageSpots = new List<StorageSpotData>();
        ReservedCraftingSpots = new List<CraftingSpotData>();
        ReservedCraftingResourceStorageSpots = new List<StorageSpotData>();
    }

    public virtual void Start()
    {
        TaskState = TaskState.Started;
        substate = 0;
    }

    public virtual void Update()
    {
        Debug.Assert(IsRunning, "Updating nonrunning task (state = " + TaskState + ")");
        Debug.Assert(Worker.AssignedBuilding != null, "Failed to cancel task when assigned building cleared");
    }

    protected virtual void CompleteTask()
    {
        Cleanup();
        TaskState = TaskState.Completed;
        Worker.OnTaskCompleted(false);
    }

    public virtual void Abandon()
    {
        Cleanup();
        TaskState = TaskState.Abandoned;
        Worker.OnTaskCompleted(true);
    }

    protected void gotoSubstate(int num)
    {
        substate = num;
        timeStartedSubstate = GameTime.time;
        //  Update();
    }

    protected void GotoNextSubstate() => gotoSubstate(substate + 1);

    public bool IsSubstateDone(float substateRuntime) => getPercentSubstateDone(substateRuntime) == 1;

    public float getPercentSubstateDone(float substateRuntime)
    {
        return Math.Clamp((GameTime.time - timeStartedSubstate) / (substateRuntime / GameTime.timeScale), 0, 1);
    }

    // ==== GATHERING ===================================================

    protected GatheringSpotData reserveClosestBuildingGatheringSpot(BuildingData buildingGatheringFrom, LocationComponent location)
    {
        var spot = buildingGatheringFrom.ReserveClosestGatheringSpot(Worker, location);
        Debug.Assert(spot != null, "Failed to reserve gathering spot in " + buildingGatheringFrom.DefnId);
        reserveGatheringSpot(spot);
        return spot;
    }

    protected void reserveGatheringSpot(GatheringSpotData spot)
    {
        spot.Reservation.ReserveBy(Worker);
        ReservedGatheringSpots.Add(spot);
    }

    protected void unreserveGatheringSpot(GatheringSpotData spot)
    {
        spot.Reservation.Unreserve();
        ReservedGatheringSpots.Remove(spot);
    }


    // ==== STORAGE ===================================================

    protected StorageSpotData reserveStorageSpotClosestToWorldLoc_AssignedBuildingOrPrimaryStorageOnly(LocationComponent location)
    {
        var spot = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, location, Worker);
        Debug.Assert(spot != null, "Caller neesd to ensure that we can reserve storage spot close to " + location);
        Debug.Assert(!ReservedStorageSpots.Contains(spot), "Reserved spot " + spot.InstanceId + " already in ReservedStorageSpots");
        reserveStorageSpot(spot);
        return spot;
    }

    protected StorageSpotData reserveStorageSpotClosestToWorldLoc(LocationComponent location)
    {
        var spot = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Any, location);
        Debug.Assert(spot != null, "Failed to reserve storage spot close to " + location);
        Debug.Assert(!ReservedStorageSpots.Contains(spot), "Reserved spot " + spot.InstanceId + " already in ReservedStorageSpots");
        reserveStorageSpot(spot);
        return spot;
    }

    protected StorageSpotData reserveStorageSpot(BuildingData buildingToStoreIn)
    {
        var spot = buildingToStoreIn.ReserveStorageSpot(Worker);
        Debug.Assert(spot != null, "Failed to reserve storage spot in " + buildingToStoreIn.DefnId);
        Debug.Assert(!ReservedStorageSpots.Contains(spot), "Reserved spot " + spot.InstanceId + " already in ReservedStorageSpots");

        ReservedStorageSpots.Add(spot);
        return spot;
    }

    protected StorageSpotData reserveStorageSpot(StorageSpotData spot)
    {
        Debug.Assert(spot != null, "Failed to reserve storage spot in " + spot.Building.DefnId);
        Debug.Assert(!ReservedStorageSpots.Contains(spot), "Reserved spot " + spot.InstanceId + " already in ReservedStorageSpots");
        spot.Reservation.ReserveBy(Worker);
        ReservedStorageSpots.Add(spot);
        return spot;
    }

    protected void unreserveStorageSpot(StorageSpotData spot)
    {
        spot.Reservation.Unreserve();
        ReservedStorageSpots.Remove(spot);
    }


    // ==== CRAFTING ===================================================

    protected CraftingSpotData reserveCraftingSpot(CraftingSpotData spot)
    {
        // var spot = buildingToCraftIn.ReserveCraftingSpot(Worker);
        // Debug.Assert(spot != null, "Failed to reserve crafting spot in " + buildingToCraftIn.DefnId);

        Debug.Assert(!ReservedCraftingSpots.Contains(spot), "Reserved spot " + spot.InstanceId + " already in ReservedCraftingSpots");
        spot.Reservation.ReserveBy(Worker);
        ReservedCraftingSpots.Add(spot);
        return spot;
    }

    protected void unreserveCraftingSpot(CraftingSpotData spot)
    {
        spot.Reservation.Unreserve();
        ReservedCraftingSpots.Remove(spot);
    }


    // ==== CRAFTING RESOURCES =========================================

    protected bool HasMoreCraftingResourcesToGet()
    {
        return ReservedCraftingResourceStorageSpots.Count > 0;
    }

    protected void reserveCraftingResourceStorageSpotForItem(ItemDefn itemDefn)
    {
        var spot = Worker.AssignedBuilding.GetStorageSpotWithUnreservedItem(itemDefn);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.AssignedBuilding.DefnId);

        spot.Reservation.ReserveBy(Worker);
        ReservedCraftingResourceStorageSpots.Add(spot);
    }

    protected void unreserveBuildingCraftingResourceSpot(StorageSpotData spot)
    {
        spot.Reservation.Unreserve();
        ReservedCraftingResourceStorageSpots.Remove(spot);
    }


    protected StorageSpotData getNextReservedCraftingResourceStorageSpot()
    {
        Debug.Assert(ReservedCraftingResourceStorageSpots.Count > 0, "getting crafting resource spot, but none remain");
        return ReservedCraftingResourceStorageSpots[0];
    }

    public virtual void Cleanup()
    {
        foreach (var spot in ReservedStorageSpots)
            spot.Reservation.Unreserve();
        foreach (var spot in ReservedGatheringSpots)
            spot.Reservation.Unreserve();
        foreach (var spot in ReservedCraftingSpots)
            spot.Reservation.Unreserve();
        foreach (var spot in ReservedCraftingResourceStorageSpots)
            spot.Reservation.Unreserve();
    }

    protected bool MoveTowards(LocationComponent location, float distanceMovedPerSecond, float closeEnough = .1f)
    {
        LastMoveToTarget.SetWorldLoc(location);

        // Move towards target
        Worker.Location.MoveTowards(Worker.Location, location, distanceMovedPerSecond * GameTime.deltaTime);

        float distance = Worker.Location.DistanceTo(location);
        if (distance <= closeEnough)
        {
            Worker.Location.SetWorldLoc(location);
            return true; // reached dest
        }

        return false; // not reached
    }

    protected bool moveTowards(BuildingData target, float distanceMovedPerSecond)
    {
        return MoveTowards(target.Location, distanceMovedPerSecond);
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public virtual void OnBuildingDestroyed(BuildingData building)
    {
    }

    // Called when any building is moved; if this Task involves that building then determine
    // what we should do (if anything).
    public virtual void OnBuildingMoved(BuildingData building, LocationComponent previousLocw)
    {
    }

    // Called when any building is paused; if this Task involves that building then determine
    // what we should do (if anything).
    public virtual void OnBuildingPauseToggled(BuildingData building)
    {
    }

    protected StorageSpotData getBetterStorageSpotThanSpotIfExists(StorageSpotData sourceSpot)
    {
        float distanceToSourceSpot = Worker.Location.DistanceTo(sourceSpot.Location);
        var closestSpot = sourceSpot.Building.GetClosestEmptyStorageSpot(Worker.Location, out float distanceToNewSpot);
        if (distanceToNewSpot >= distanceToSourceSpot)
            return sourceSpot;// there isn't a better one

        // found a better one
        unreserveStorageSpot(sourceSpot);
        return reserveStorageSpot(closestSpot);
    }

    protected StorageSpotData getBetterStorageSpotThanSpotIfExists_AssignedBuildingOrPrimaryStorageOnly(StorageSpotData sourceSpot)
    {
        var bestSpot = getClosestBestStorageSpot_AssignedBuildingOrPrimaryStorageOnly(out float distanceToBestSpot);

        var distanceToReservedStorageSpot = Worker.Location.DistanceTo(sourceSpot.Location);
        if (distanceToBestSpot < distanceToReservedStorageSpot)
        {
            unreserveStorageSpot(sourceSpot);
            reserveStorageSpot(bestSpot);
            return bestSpot;
        }

        return sourceSpot;
    }

    private StorageSpotData getClosestBestStorageSpot_AssignedBuildingOrPrimaryStorageOnly(out float distance)
    {
        var closestAssignedBuildingSpot = Worker.AssignedBuilding.GetClosestEmptyStorageSpot(Worker.Location, out float distanceToClosestAssignedBuildingSpot);
        var closestPrimaryStorageSpot = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Primary, Worker.Location, null, out float distanceToClosestPrimaryStorageSpot);
        if (distanceToClosestAssignedBuildingSpot < distanceToClosestPrimaryStorageSpot)
        {
            distance = distanceToClosestAssignedBuildingSpot;
            return closestAssignedBuildingSpot;
        }
        else
        {
            distance = distanceToClosestPrimaryStorageSpot;
            return closestPrimaryStorageSpot;
        }
    }

    protected StorageSpotData FindNewOptimalStorageSpotToDeliverItemTo(StorageSpotData originalReservedSpot, LocationComponent closestLocation)
    {
        var optimalStorageSpotToDeliverItemTo = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, closestLocation, Worker);
        if (optimalStorageSpotToDeliverItemTo == null)
            return originalReservedSpot;

        if (optimalStorageSpotToDeliverItemTo != originalReservedSpot)
        {
            originalReservedSpot.Reservation.Unreserve();
            originalReservedSpot = optimalStorageSpotToDeliverItemTo;
            originalReservedSpot.Reservation.ReserveBy(Worker);
        }
        return originalReservedSpot;
    }
}
