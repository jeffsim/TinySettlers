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

    [SerializeField] List<IReservationProvider> SpotsToReserveOnStart;
    [SerializeField] protected List<IReservationProvider> ReservedSpots;

    public bool HasReservedSpot(IReservationProvider spot) => ReservedSpots.Contains(spot);

    public abstract string ToDebugString();

    public virtual bool IsCarryingItem(string itemId) => false;

    [SerializeField] protected List<StorageSpotData> ReservedCraftingResourceStorageSpots;
    // [SerializeField] protected List<CraftingSpotData> ReservedCraftingSpots;

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
        Need = need;
        Worker = workerData;

        SpotsToReserveOnStart = new();
        ReservedSpots = new();

        ReservedCraftingResourceStorageSpots = new();
    }

    public T ReserveSpotOnStart<T>(T spot) where T : IReservationProvider
    {
        SpotsToReserveOnStart.Add(spot);
        return spot;
    }

    public virtual void Start()
    {
        TaskState = TaskState.Started;
        substate = 0;

        foreach (var spot in SpotsToReserveOnStart)
            reserveSpot(spot);
        SpotsToReserveOnStart.Clear();
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

    protected void GotoSubstate(int num)
    {
        substate = num;
        timeStartedSubstate = GameTime.time;
    }

    protected void GotoNextSubstate() => GotoSubstate(substate + 1);

    public bool IsSubstateDone(float substateRuntime) => getPercentSubstateDone(substateRuntime) == 1;

    public float getPercentSubstateDone(float substateRuntime)
    {
        return Math.Clamp((GameTime.time - timeStartedSubstate) / (substateRuntime / GameTime.timeScale), 0, 1);
    }

    private void reserveSpot(IReservationProvider spot)
    {
        spot.Reservation.ReserveBy(Worker);
        ReservedSpots.Add(spot); // keep track so that we can automatically unreserve them when the Task is done
    }

    protected void unreserveSpot(IReservationProvider spot)
    {
        spot.Reservation.Unreserve();
        ReservedSpots.Remove(spot);
    }

    public virtual void Cleanup()
    {
        foreach (var spot in ReservedSpots)
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
