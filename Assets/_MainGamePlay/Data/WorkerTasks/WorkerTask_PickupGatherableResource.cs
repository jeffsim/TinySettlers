using System;
using UnityEngine;

public enum WorkerTask_PickupGatherableResourceSubstate
{
    GotoGatheringSpot = 0,
    ReapGatherableResource = 1,
    PickupGatherableResource = 2,
};

[Serializable]
public class WorkerTask_PickupGatherableResource : WorkerTask
{
    public override string ToString() => "Pickup gatherable resource";
    internal override string GetDebuggerString() => $"Pickup gatherable resource from {OptimalGatheringSpot}";

    public override TaskType Type => TaskType.PickupGatherableResource;

    [SerializeField] public GatheringSpotData OptimalGatheringSpot;
    [SerializeField] public StorageSpotData ReservedStorageSpot;

    public const float secondsToReap = 1;
    public const float secondsToPickup = 0.5f;

    public override bool IsWalkingToTarget => substate == 0;

    public override ItemDefn GetTaskItem() => OptimalGatheringSpot?.ItemContainer.Item.Defn;

    // TODO: Pooling
    public static WorkerTask_PickupGatherableResource Create(WorkerData worker, NeedData needData, GatheringSpotData optimalGatheringSpot, StorageSpotData storageSpotToReserve)
    {
        return new(worker, needData, optimalGatheringSpot, storageSpotToReserve);
    }

    private WorkerTask_PickupGatherableResource(WorkerData worker, NeedData needData, GatheringSpotData optimalGatheringSpot, StorageSpotData storageSpotToReserve) : base(worker, needData)
    {
        OptimalGatheringSpot = ReserveSpotOnStart(optimalGatheringSpot);
        ReservedStorageSpot = ReserveSpotOnStart(storageSpotToReserve);
    }

    // Note: this is called when any building is destroyed, not just "this task's" building
    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If our target resource-gathering building was destroyed and then abandon
        if (destroyedBuilding == OptimalGatheringSpot.Building && substate < 2)
        {
            Abandon();
            return;
        }

        // If the building which we have reserved a storage spot in was destroyed then try to find an alternative
        if (destroyedBuilding == ReservedStorageSpot.Building)
        {
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(ReservedStorageSpot, Worker.Location);
            if (newSpot == null)
                Abandon(); // Failed to find an alternative.  TODO: Test this; e.g. town storage is full, destroy building that last item is being delivered to.
            else
            {
                // Swap for new storage spot
                ReservedSpots.Remove(ReservedStorageSpot);
                ReservedStorageSpot = newSpot;
                ReservedSpots.Add(ReservedStorageSpot);
            }
        }
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        if (building != OptimalGatheringSpot.Building) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location - previousLoc;
        else
            Worker.Location += building.Location - previousLoc;
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(ReservedStorageSpot, OptimalGatheringSpot.Location);
        if (newSpot != ReservedStorageSpot)
        {
            ReservedSpots.Remove(ReservedStorageSpot);
            ReservedStorageSpot = newSpot;
            ReservedSpots.Add(ReservedStorageSpot);
        }

        // If our worker's building is the one that was paused then cancel this task regardless of substate
        if (Worker.Assignment.AssignedTo == building)
        {
            Worker.AI.CurrentTask.Abandon();
            return;
        }

        // If the building from which we are gathering was paused then abandon this task
        if (building == OptimalGatheringSpot.Building)
        {
            Worker.AI.CurrentTask.Abandon();
            return;
        }
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot: // go to resource spot
                if (MoveTowards(OptimalGatheringSpot.Location))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource: // reap item (e.g. cut down tree)
                if (IsSubstateDone(secondsToReap))
                {
                    // Done reaping.
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource: // gather in the building.
                if (IsSubstateDone(secondsToPickup))
                {
                    // Remove item from gathering spot and put it in Worker's hand, and we're done
                    CompleteTask();
                    Worker.Hands.SetItem(OptimalGatheringSpot.ItemContainer.ClearItem());

                    // NOTE that completing the task unreserved both the gathering spot and the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    Worker.StorageSpotReservedForItemInHand = ReservedStorageSpot;
                    Worker.OriginalPickupItemNeed = Need;
                    ReservedStorageSpot.Reservation.ReserveBy(Worker);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}