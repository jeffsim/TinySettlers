using System;
using UnityEngine;

public enum WorkerTask_PickupItemFromBuildingSubstate
{
    GotoStorageSpotWithItem = 0,
    PickupItemFromStorageSpot = 1,
};

[Serializable]
public class WorkerTask_PickupItemFromBuilding : WorkerTask
{
    public override string ToString() => "Pickup item from storage spot";
    internal override string getDebuggerString() => $"Pickup item from {spotWithItemToPickup}";

    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    [SerializeField] StorageSpotData spotWithItemToPickup;
    [SerializeField] StorageSpotData reservedStorageSpot;

    public const float secondsToPickup = 0.5f;

    public override bool Debug_IsMovingToTarget => substate == 0;

    public override ItemDefn GetTaskItem() => spotWithItemToPickup.ItemInSpot.Defn;

    public override string ToDebugString()
    {
        var str = "Pickup item from storage\n";
        str += "  Pick up from: " + spotWithItemToPickup + " (" + spotWithItemToPickup.Building + "), gatherspot: " + spotWithItemToPickup.InstanceId + "\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_PickupItemFromBuildingSubstate.GotoStorageSpotWithItem: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, spotWithItemToPickup.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_PickupItemFromBuildingSubstate.PickupItemFromStorageSpot: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_PickupItemFromBuilding Create(WorkerData worker, NeedData needData, StorageSpotData spotWithItem, StorageSpotData storageSpotToReserve)
    {
        return new WorkerTask_PickupItemFromBuilding(worker, needData, spotWithItem, storageSpotToReserve);
    }

    private WorkerTask_PickupItemFromBuilding(WorkerData worker, NeedData needData, StorageSpotData spotWithItem, StorageSpotData storageSpotToReserve) : base(worker, needData)
    {
        this.spotWithItemToPickup = spotWithItem;

        // While this task is simply to go pick up an item, we wouldn't start the task if we didn't know that there was at least one place that we could bring the
        // resource to; we reserve that so that if no building needs it after we pick it up, we can still store it somewhere
        reservedStorageSpot = storageSpotToReserve;
    }

    public override void Start()
    {
        base.Start();

        // Now that we've actually started the task, we can reserve the spots that were passed in above.
        reserveStorageSpot(spotWithItemToPickup);
        reserveStorageSpot(reservedStorageSpot);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // If our target resource-gathering building was destroyed and we're still walking to it, or gathering from it, then abandon
        if (spotWithItemToPickup.Building.IsDestroyed && substate == 0)
            Abandon();
        else if (reservedStorageSpot.Building.IsDestroyed && substate == 0)
            Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_PickupItemFromBuildingSubstate)substate)
        {
            case WorkerTask_PickupItemFromBuildingSubstate.GotoStorageSpotWithItem: updateMoveToLoc = building == spotWithItemToPickup.Building; break;
            case WorkerTask_PickupItemFromBuildingSubstate.PickupItemFromStorageSpot: updateWorkerLoc = building == spotWithItemToPickup.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupItemFromBuildingSubstate.GotoStorageSpotWithItem: // go to resource spot
                if (moveTowards(spotWithItemToPickup.WorldLoc, distanceMovedPerSecond))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_PickupItemFromBuildingSubstate.PickupItemFromStorageSpot: // gather in the building.
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    // Remove item from gathering spot and put it in Worker's hand, and we're done
                    CompleteTask();
                    Worker.AddItemToHands(spotWithItemToPickup.RemoveItem());

                    // NOTE that completing the task unreserved both the gathering spot and the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    Worker.StorageSpotReservedForItemInHand = reservedStorageSpot;
                    Worker.OriginalPickupItemNeed = Need;
                    reservedStorageSpot.ReserveBy(Worker);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}