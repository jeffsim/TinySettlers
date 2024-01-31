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
    public override bool IsWalkingToTarget => substate == 0;

    public override ItemDefn GetTaskItem() => Worker.ItemInHand?.Defn;
    public override bool IsCarryingItem(string itemId) => true;

    public override string ToDebugString()
    {
        var str = "Deliver\n";
        str += "  Item: " + Worker.ItemInHand.DefnId + "\n";
        str += "  TargetStorage: " + Worker.StorageSpotReservedForItemInHand.InstanceId + " (" + Worker.StorageSpotReservedForItemInHand.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo: str += "; dist: " + Vector2.Distance(Worker.Location.WorldLoc, Worker.StorageSpotReservedForItemInHand.Location.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public static WorkerTask_DeliverItemInHandToStorageSpot CreateAndStart(WorkerData worker, NeedData need) => new(worker, need);

    private WorkerTask_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need) : base(worker, need)
    {
        worker.CurrentTask = this;
        base.Start();
    }

    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If the building which we have reserved a storage spot in was destroyed then try to find an alternative
        if (destroyedBuilding == Worker.StorageSpotReservedForItemInHand.Building)
        {
            Worker.StorageSpotReservedForItemInHand = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            substate = 0; // back to walking again
            if (Worker.StorageSpotReservedForItemInHand == null)
                Abandon(); // Failed to find an alternative.  TODO: Test this; e.g. town storage is full, destroy building that last item is being delivered to.
        }
    }

    public override void OnBuildingMoved(BuildingData movedBuilding, Vector3 previousWorldLoc)
    {
        // If we're still walking, then determine if there is now a better/closer storage spot to deliver the item to. e.g. if user moved building far away
        // Note that we do this even if a building other than our target building moved, since a better alternative may have moved closer the worker.
        if (IsWalkingToTarget)
        {
            Worker.StorageSpotReservedForItemInHand = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            if (Worker.StorageSpotReservedForItemInHand == null)
                Debug.Assert(false, "Failed to find *any* spot to store in.  Shouldn't happen since we already had one reserved");
        }

        // If we're standing still and working in the building that was moved, then update our location
        if (substate == (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot && movedBuilding == Worker.StorageSpotReservedForItemInHand.Building)
            Worker.Location.WorldLoc += movedBuilding.Location.WorldLoc - previousWorldLoc;
    }

    public override void OnBuildingPauseToggled(BuildingData movedBuilding)
    {
        // If we're still moving then determine if there is now a better/closer storage spot to deliver the item to.
        if (IsWalkingToTarget)
        {
            Worker.StorageSpotReservedForItemInHand = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            if (Worker.StorageSpotReservedForItemInHand == null)
                Debug.Assert(false, "Failed to find *any* spot to store in.  Shouldn't happen since we already had one reserved");
        }
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo: // Walk back to our assigned building
                if (MoveTowards(Worker.StorageSpotReservedForItemInHand.Location.WorldLoc, Worker.GetMovementSpeed()))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot: // drop the gathered item; and then done.
                if (IsSubstateDone(secondsToDrop))
                {
                    // Done dropping.  Add the item into the storage spot.
                    Worker.DropItemInHandInReservedStorageSpot();
                    CompleteTask();
                }
                break;

            default: Debug.LogError("unknown substate " + substate); break;
        }
    }

    protected override void CompleteTask()
    {
        Need.UnassignWorkerToMeetNeed(Worker);
        base.CompleteTask();
    }
}