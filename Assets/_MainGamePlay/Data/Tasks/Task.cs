using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType
{
    Unset,
    Idle,
    DeliverItemInHandToStorageSpot,
    GatherResource,
    TransportItemFromSpotToSpot,
    PickupItemFromGround,
    SellItem,
    Task_CraftItem
};

public enum TaskState { Unset, NotStarted, Started, Completed, Abandoned };

[Serializable]
public abstract class Task
{
    public override string ToString() => $"{Type} ToString() not implemented"; // used for VS Code debugging pane

    // == Task State ===========================
    public virtual TaskType Type => TaskType.Unset;
    public TaskState TaskState = TaskState.Unset;
    public bool IsRunning => TaskState == TaskState.Started;
    public bool IsAbandoned => TaskState == TaskState.Abandoned;

    // == Worker ===============================
    [SerializeReference] public WorkerData Worker;

    // == Subtasks =============================
    public List<Subtask> Subtasks = new();
    [SerializeField] public Subtask CurSubTask;
    public int SubtaskIndex = 0;

    // == Movement and Location ================
    public virtual bool IsWalkingToTarget => CurSubTask.IsWalkingToTarget;
    public LocationComponent LastMoveToTarget = new();

    // == Reservable Spots =====================
    [SerializeField] protected List<IReservationProvider> SpotsToReserveOnStart = new();
    [SerializeField] public List<IReservationProvider> ReservedSpots = new();
    public bool HasReservedSpot(IReservationProvider spot) => ReservedSpots.Contains(spot);

    // == Items ================================
    public virtual bool IsCarryingItem(string itemId) => false;
    public virtual ItemDefn GetTaskItem() => CurSubTask.GetTaskItem();

    // == Need =================================
    // The Need that this task is meeting
    public NeedData Need;

    // ====================================================================
    // Constructor
    protected Task(WorkerData workerData, NeedData need)
    {
        Need = need;
        Worker = workerData;
    }

    public virtual void Start()
    {
        TaskState = TaskState.Started;

        foreach (var spot in SpotsToReserveOnStart)
            ReserveSpot(spot);

        SubtaskIndex = 0;
        CurSubTask = GetNextSubtask();
        CurSubTask.Start();
    }

    public virtual void GotoNextSubstate()
    {
        CurSubTask?.SubtaskComplete();

        SubtaskIndex++;
        CurSubTask = GetNextSubtask();
        if (CurSubTask == null)
            AllSubtasksComplete();
        else
        {
            CurSubTask.Start();
            if (CurSubTask.InstantlyComplete)
                GotoNextSubstate();
        }
    }

    public virtual void OnSubtaskStart() { }
    public virtual void InitializeStateMachine() { }

    public virtual Subtask GetNextSubtask()
    {
        return null;
    }

    // ====================================================================
    // Called per AI tick.
    public virtual void Update()
    {
        Debug.Assert(IsRunning, "Updating nonrunning task (state = " + TaskState + ")");
        Debug.Assert(Worker.Assignment.IsAssigned, "Failed to cancel task when assigned building cleared");
        CurSubTask?.Update();
    }


    // ====================================================================================================================
    // Spot reservations

    public T ReserveSpotOnStart<T>(T spot) where T : IReservationProvider
    {
        Debug.Assert(!ReservedSpots.Contains(spot), $"Task is trying to reserve {spot} on Start but it's already reserved");
        SpotsToReserveOnStart.Add(spot);
        return spot;
    }

    protected IReservationProvider ReserveSpot(IReservationProvider spot)
    {
        Debug.Assert(spot != null, "Trying to reserve a null spot");
        spot.Reservation.ReserveBy(Worker);

        Debug.Assert(!ReservedSpots.Contains(spot), $"Task is trying to reserve {spot} but it's already reserved by {Worker}");
        ReservedSpots.Add(spot);
        return spot;
    }

    public void UnreserveSpot(IReservationProvider spot)
    {
        spot.Reservation.Unreserve();

        Debug.Assert(ReservedSpots.Contains(spot), $"Task is trying to unreserve {spot} but it isn't reserved by {Worker}");
        ReservedSpots.Remove(spot);
    }


    // ====================================================================================================================
    // Worker movement

    public bool MoveTowards(LocationComponent location, float closeEnough = .1f)
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

    public virtual void CompleteTask() => finishTask(TaskState.Completed);
    public virtual void Abandon() => finishTask(TaskState.Abandoned);
    void finishTask(TaskState newState)
    {
        ReservedSpots.ForEach(spot => spot.Reservation.Unreserve());
        TaskState = newState;
        Worker.AI.StartIdling();
    }

    // ====================================================================================================================
    // Building status updates

    public virtual void OnBuildingPauseToggled(BuildingData building)
    {
        if (CurSubTask.AutomaticallyAbandonIfAssignedBuildingPaused && Worker.Assignment.AssignedTo == building)
            Abandon();
        else
            CurSubTask.OnAnyBuildingPauseToggled(building);
    }

    public virtual void OnBuildingDestroyed(BuildingData building)
    {
        if (CurSubTask.AutomaticallyAbandonIfAssignedBuildingDestroyed && Worker.Assignment.AssignedTo == building)
            Abandon();
        else
            CurSubTask.OnAnyBuildingDestroyed(building);
    }

    public virtual void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        if (CurSubTask.AutomaticallyAbandonIfAssignedBuildingMoved && Worker.Assignment.AssignedTo == building)
        {
            Abandon();
            return;
        }
        var offset = building.Location - previousLoc;
        if (CurSubTask.UpdateWorkerLocWhenTheseBuildingsMove.Contains(building))
            Worker.Location += offset;
        if (CurSubTask.UpdateMoveTargetWhenTheseBuildingsMove.Contains(building))
            LastMoveToTarget += offset;

        CurSubTask.OnAnyBuildingMoved(building, previousLoc);
    }

    public virtual void AllSubtasksComplete()
    {
        TaskState = TaskState.Completed;
        CompleteTask();
    }


    // ====================================================================================================================
    // Other functions

    protected IItemSpotInBuilding FindAndReserveNewOptimalStorageSpot(IItemSpotInBuilding originalReservedSpot, LocationComponent closestLocation, bool updateMoveLoc)
    {
        // Temporarily unreserve the spot so that it can be returned if closest
        var reservedBy = originalReservedSpot.Reservation.ReservedBy;
        originalReservedSpot.Reservation.Unreserve();
        var optimalStorageSpotToDeliverItemTo = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, closestLocation, Worker);
        originalReservedSpot.Reservation.ReserveBy(reservedBy);
        if (optimalStorageSpotToDeliverItemTo == null && originalReservedSpot.Building.IsPaused)
        {
            // Couldn't find any and original spot is no longer valid
            return null;
        }

        if (optimalStorageSpotToDeliverItemTo != null && optimalStorageSpotToDeliverItemTo != originalReservedSpot)
        {
            UnreserveSpot(originalReservedSpot);
            originalReservedSpot = optimalStorageSpotToDeliverItemTo;
            ReserveSpot(originalReservedSpot);
            if (updateMoveLoc)
                LastMoveToTarget.SetWorldLoc(originalReservedSpot.Location);
        }
        return originalReservedSpot;
    }

    protected IItemSpotInBuilding FindAndReserveOptimalStorageSpot(LocationComponent closestLocation, bool isCurrentMoveTarget)
    {
        var optimalSpot = Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, closestLocation, Worker);
        if (optimalSpot == null)
            return null;
        ReserveSpot(optimalSpot);
        if (isCurrentMoveTarget)
            LastMoveToTarget.SetWorldLoc(optimalSpot.Location);
        return optimalSpot;
    }

    // protected IItemSpotInBuilding FindAndReserveOptimalGatheringSpot(LocationComponent closestLocation, ItemDefn itemDefn, bool isCurrentMoveTarget)
    // {
    //     var optimalSpot = Worker.Town.GetClosestAvailableGatheringSpot(closestLocation, itemDefn, Worker);
    //     if (optimalSpot == null)
    //         return null;
    //     ReserveSpot(optimalSpot);
    //     if (isCurrentMoveTarget)
    //         LastMoveToTarget.SetWorldLoc(optimalSpot.Location);
    //     return optimalSpot;
    // }
    // protected IItemSpotInBuilding FindAndReserveNewOptimalGatheringSpot(IItemSpotInBuilding originalReservedSpot, LocationComponent closestLocation, ItemDefn itemDefn, bool isCurrentMoveTarget)
    // {
    //     // Temporarily unreserve the spot so that it can be returned if closest
    //     var reservedBy = originalReservedSpot.Reservation.ReservedBy;
    //     originalReservedSpot.Reservation.Unreserve();
    //     var optimalStorageSpotToDeliverItemTo = Worker.Town.GetClosestAvailableGatheringSpot(closestLocation, itemDefn, Worker);
    //     originalReservedSpot.Reservation.ReserveBy(reservedBy);
    //     if (optimalStorageSpotToDeliverItemTo == null && originalReservedSpot.Building.IsPaused)
    //     {
    //         // Couldn't find any and original spot is no longer valid
    //         return null;
    //     }

    //     if (optimalStorageSpotToDeliverItemTo != null && optimalStorageSpotToDeliverItemTo != originalReservedSpot)
    //     {
    //         UnreserveSpot(originalReservedSpot);
    //         originalReservedSpot = optimalStorageSpotToDeliverItemTo;
    //         ReserveSpot(originalReservedSpot);
    //         if (isCurrentMoveTarget)
    //             LastMoveToTarget.SetWorldLoc(originalReservedSpot.Location);
    //     }
    //     return originalReservedSpot;
    // }
}
