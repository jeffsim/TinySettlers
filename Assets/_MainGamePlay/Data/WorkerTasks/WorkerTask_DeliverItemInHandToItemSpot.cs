using System;
using UnityEngine;

public enum WorkerTask_DeliverItemInHandToStorageSpotSubstate
{
    GotoStorageSpotToDeliverItemTo = 0,
    DropItemInDestinationStorageSpot = 1
};

[Serializable]
public class WorkerTask_DeliverItemInHandToStorageSpot : WorkerTask
{
    public override string ToString() => "Deliver item in hand to storage spot";
    public override TaskType Type => TaskType.DeliverItemInHandToStorageSpot;

    public const float secondsToPickup = 1;
    public const float secondsToDrop = 0.5f;
    public override bool Debug_IsMovingToTarget => substate == 0 || substate == 2;

    public override ItemDefn GetTaskItem() => Worker.ItemInHand?.Defn;
    public override bool IsCarryingItem(string itemId) => substate == 0 && Worker.ItemInHand.DefnId == itemId;

    public override string ToDebugString()
    {
        var str = "Deliver\n";
        str += "  Item: " + Worker.ItemInHand.DefnId + "\n";
        str += "  TargetStorage: " + Worker.StorageSpotReservedForItemInHand.InstanceId + " (" + Worker.StorageSpotReservedForItemInHand.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, Worker.StorageSpotReservedForItemInHand.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public static WorkerTask_DeliverItemInHandToStorageSpot CreateAndStart(WorkerData worker, NeedData need) => new WorkerTask_DeliverItemInHandToStorageSpot(worker, need);

    private WorkerTask_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need) : base(worker, need)
    {
        worker.CurrentTask = this;
        
        base.Start();

        // worker.StorageSpotReservedForItemInHand has already been reserved by caller; we track it as a reserved storage spot so that we can automatically unreserve it
        // when needed (e.g. if the building is destroyed, task is completed, worker dies, ...)
        ReservedStorageSpots.Add(worker.StorageSpotReservedForItemInHand);
    }

    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If the building to which we are bringing the item is destroyed then abandon
        if (destroyedBuilding == Worker.StorageSpotReservedForItemInHand.Building)
        {
            Need.UnassignWorkerToMeetNeed(Worker);
            Abandon();
        }
    }

    public override void OnBuildingMoved(BuildingData movedBuilding, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_DeliverItemInHandToStorageSpotSubstate)substate)
        {
            case WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo: updateMoveToLoc = movedBuilding == Worker.StorageSpotReservedForItemInHand.Building; break;
            case WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot: updateWorkerLoc = movedBuilding == Worker.StorageSpotReservedForItemInHand.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += movedBuilding.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += movedBuilding.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo: // Walk back to our assigned building
                if (moveTowards(Worker.StorageSpotReservedForItemInHand.WorldLoc, Worker.GetMovementSpeed()))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot: // drop the gathered item; and then done.
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // Done dropping.  Add the item into the storage spot.
                    Worker.DropItemInHandInReservedStorageSpot();
                    CompleteTask();
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }

    protected override void CompleteTask()
    {
        Need.UnassignWorkerToMeetNeed(Worker);
        base.CompleteTask();
    }
}