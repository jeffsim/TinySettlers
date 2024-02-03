using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorkerTask_PickupItemFromStorageSpotSubstate
{
    GotoItemSpotWithItem = 0,
    PickupItemFromItemSpot = 1,
};

[Serializable]
public class WorkerTask_PickupItemFromStorageSpot : WorkerTask
{
    public override string ToString() => "Pickup item from item spot";
    internal override string getDebuggerString() => $"Pickup item from {spotWithItemToPickup}";

    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    [SerializeField] StorageSpotData spotWithItemToPickup;
    [SerializeField] StorageSpotData reservedSpotToStoreItemIn;

    public const float secondsToPickup = 0.5f;

    public override bool IsWalkingToTarget => substate == 0;

    public override ItemDefn GetTaskItem() => spotWithItemToPickup.ItemContainer.Item.Defn;

    public override string ToDebugString()
    {
        var str = "Pickup item from itemspot\n";
        str += "  Pick up from: " + spotWithItemToPickup + " (" + spotWithItemToPickup.Building + "), gatherspot: " + spotWithItemToPickup.InstanceId + "\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem: str += "; dist: " + Worker.Location.DistanceTo(spotWithItemToPickup.Location).ToString("0.0"); break;
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_PickupItemFromStorageSpot Create(WorkerData worker, NeedData needData, StorageSpotData spotWithItem, StorageSpotData spotToReserve)
    {
        return new(worker, needData, spotWithItem, spotToReserve);
    }

    private WorkerTask_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToPickup, StorageSpotData reservedSpotToStoreItemIn) : base(worker, needData)
    {
        this.spotWithItemToPickup = ReserveSpotOnStart(spotWithItemToPickup);
        this.reservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    // Note: this is called when any building is destroyed, not just "this task's" building
    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If our target spot's building was destroyed and we're still walking to it, then abandon
        if (destroyedBuilding == spotWithItemToPickup.Building && substate == (int)WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem)
        {
            Abandon();
            return;
        }

        // If the building which we have reserved a storage spot in was destroyed then try to find an alternative
        if (destroyedBuilding == reservedSpotToStoreItemIn.Building)
        {
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(reservedSpotToStoreItemIn, Worker.Location);
            if (newSpot == null)
                Abandon(); // Failed to find an alternative.  TODO: Test this; e.g. town storage is full, destroy building that last item is being delivered to.
            else
            {
                // Swap for new storage spot
                ReservedSpots.Remove(reservedSpotToStoreItemIn);
                reservedSpotToStoreItemIn = newSpot;
                ReservedSpots.Add(reservedSpotToStoreItemIn);
            }
        }
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        if (building != spotWithItemToPickup.Building) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location - previousLoc;
        else
            Worker.Location += building.Location - previousLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem: // go to resource spot
                if (MoveTowards(spotWithItemToPickup.Location, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot: // gather in the building.
                if (IsSubstateDone(secondsToPickup))
                {
                    // Remove item from gathering spot and put it in Worker's hand, and we're done
                    CompleteTask();
                    Worker.AddItemToHands(spotWithItemToPickup.ItemContainer.ClearItem());

                    // NOTE that completing the task unreserved both the gathering spot and the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    Worker.StorageSpotReservedForItemInHand = reservedSpotToStoreItemIn;
                    Worker.OriginalPickupItemNeed = Need;
                    reservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}