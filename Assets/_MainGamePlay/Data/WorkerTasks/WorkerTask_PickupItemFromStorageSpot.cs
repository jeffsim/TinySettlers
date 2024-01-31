using System;
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
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem: str += "; dist: " + Vector2.Distance(Worker.Location.WorldLoc, spotWithItemToPickup.Location.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_PickupItemFromStorageSpot Create(WorkerData worker, NeedData needData, StorageSpotData spotWithItem, StorageSpotData spotToReserve)
    {
        return new WorkerTask_PickupItemFromStorageSpot(worker, needData, spotWithItem, spotToReserve);
    }

    private WorkerTask_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToPickup, StorageSpotData reservedSpotToStoreItemIn) : base(worker, needData)
    {
        this.spotWithItemToPickup = spotWithItemToPickup;

        // While this task is simply to go pick up an item, we wouldn't start the task if we didn't know that there was at least one place that we could bring the
        // resource to; we reserve that so that if no building needs it after we pick it up, we can still store it somewhere
        this.reservedSpotToStoreItemIn = reservedSpotToStoreItemIn;
    }

    public override void Start()
    {
        base.Start();

        // Now that we've actually started the task, we can reserve the spots that were passed in above.
        reserveStorageSpot(spotWithItemToPickup);
        reserveStorageSpot(reservedSpotToStoreItemIn);
    }

    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If our target spot's building was destroyed and we're still walking to it, then abandon
        if (destroyedBuilding == spotWithItemToPickup.Building && substate == 0)
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
                ReservedStorageSpots.Remove(reservedSpotToStoreItemIn);
                reservedSpotToStoreItemIn = newSpot;
                ReservedStorageSpots.Add(reservedSpotToStoreItemIn);
            }
        }
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_PickupItemFromStorageSpotSubstate)substate)
        {
            case WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem: updateMoveToLoc = building == spotWithItemToPickup.Building; break;
            case WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot: updateWorkerLoc = building == spotWithItemToPickup.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.Location.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.Location.WorldLoc += building.Location.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem: // go to resource spot
                if (MoveTowards(spotWithItemToPickup.Location.WorldLoc, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot: // gather in the building.
                if (getPercentSubstateDone(secondsToPickup) == 1)
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