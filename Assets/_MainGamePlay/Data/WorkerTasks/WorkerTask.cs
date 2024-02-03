using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType
{
    Unset,
    Idle,
    DeliverItemInHandToStorageSpot,
    PickupGatherableResource,
    PickupItemInStorageSpot,
    PickupItemFromGround,
    SellItem,
    CraftGood
};

public enum TaskState { Unset, NotStarted, Started, Completed, Abandoned };

[Serializable]
public abstract class WorkerTask
{
    internal virtual string GetDebuggerString() => $"{Type}  debugger string not implemented"; // used for VS Code debugging pane

    public virtual TaskType Type => TaskType.Unset;
    public TaskState TaskState = TaskState.Unset;
    public bool IsRunning => TaskState == TaskState.Started;

    // == Worker ===============================
    [SerializeReference] public WorkerData Worker;

    // == Substate management ==================
    public float timeStartedSubstate;
    public int substate;

    // == Movement =============================
    public virtual bool IsWalkingToTarget => false;
    public LocationComponent LastMoveToTarget = new();

    // == Reservable Spots =====================
    [SerializeField] List<IReservationProvider> SpotsToReserveOnStart = new();
    [SerializeField] protected List<IReservationProvider> ReservedSpots = new();
    public bool HasReservedSpot(IReservationProvider spot) => ReservedSpots.Contains(spot);

    // == Items ================================
    public virtual bool IsCarryingItem(string itemId) => false;
    public abstract ItemDefn GetTaskItem();

    // == Need =================================
    // The Need that this task is meeting
    public NeedData Need;

    // ====================================================================
    // Constructor
    protected WorkerTask(WorkerData workerData, NeedData need)
    {
        Need = need;
        Worker = workerData;
    }

    // ====================================================================
    // Called when we have decided that this is the Task we'll perform next
    public virtual void Start()
    {
        TaskState = TaskState.Started;
        substate = 0;

        foreach (var spot in SpotsToReserveOnStart)
            ReserveSpot(spot);
        SpotsToReserveOnStart.Clear();
    }
    
    // ====================================================================
    // Called per AI tick.
    public virtual void Update()
    {
        Debug.Assert(IsRunning, "Updating nonrunning task (state = " + TaskState + ")");
        Debug.Assert(Worker.AssignedBuilding != null, "Failed to cancel task when assigned building cleared");
    }

    // ====================================================================================================================
    // Substate management

    protected void GotoSubstate(int num)
    {
        substate = num;
        timeStartedSubstate = GameTime.time;
    }

    protected void GotoNextSubstate() => GotoSubstate(substate + 1);
    protected bool IsSubstateDone(float substateRuntime) => getPercentSubstateDone(substateRuntime) == 1;
    public float getPercentSubstateDone(float substateRuntime) => Math.Clamp((GameTime.time - timeStartedSubstate) / (substateRuntime / GameTime.timeScale), 0, 1);


    // ====================================================================================================================
    // Spot reservations

    public T ReserveSpotOnStart<T>(T spot) where T : IReservationProvider
    {
        SpotsToReserveOnStart.Add(spot);
        return spot;
    }

    protected void ReserveSpot(IReservationProvider spot)
    {
        spot.Reservation.ReserveBy(Worker);

        Debug.Assert(!ReservedSpots.Contains(spot), $"Task is trying to reserve {spot} but it's already reserved by {Worker}");
        ReservedSpots.Add(spot);
    }

    protected void UnreserveSpot(IReservationProvider spot)
    {
        spot.Reservation.Unreserve();

        Debug.Assert(ReservedSpots.Contains(spot), $"Task is trying to unreserve {spot} but it isn't reserved by {Worker}");
        ReservedSpots.Remove(spot);
    }


    // ====================================================================================================================
    // Worker movement

    protected bool MoveTowards(LocationComponent location, float closeEnough = .1f)
    {
        // Track last movement target so that it can be (a) updated if buildings move and (b) rendered
        LastMoveToTarget.SetWorldLoc(location);

        // Move towards target
        Worker.Location.MoveTowards(Worker.Location, location, Worker.GetMovementSpeed() * GameTime.deltaTime);
        if (Worker.Location.WithinDistanceOf(location, closeEnough))
        {
            Worker.Location.SetWorldLoc(location);
            return true; // reached dest
        }
        return false; // not reached
    }


    // ====================================================================================================================
    // Task completion

    protected virtual void CompleteTask() => finishTask(TaskState.Completed, false);
    public virtual void Abandon() => finishTask(TaskState.Abandoned, true);

    void finishTask(TaskState newState, bool abandoned)
    {
        ReservedSpots.ForEach(spot => spot.Reservation.Unreserve());
        TaskState = newState;
        Worker.OnTaskCompleted(abandoned);
    }

    // ====================================================================================================================
    // Building status updates

    // Called when any building is destroyed, moved, or paused; if this Task involves that building then determine what we should do (if anything).
    public virtual void OnBuildingDestroyed(BuildingData building) { }
    public virtual void OnBuildingMoved(BuildingData building, LocationComponent previousLoc) { }
    public virtual void OnBuildingPauseToggled(BuildingData building) { }


    // ====================================================================================================================
    // Other functions

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
