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

    public override ItemDefn GetTaskItem() => Worker.Hands.HasItem ? Worker.Hands.Item.Defn : null;
    public override bool IsCarryingItem(string itemId) => true;

    public static WorkerTask_DeliverItemInHandToStorageSpot CreateAndStart(WorkerData worker, NeedData need) => new(worker, need);

    private WorkerTask_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need) : base(worker, need)
    {
        base.Start();
        worker.CurrentTask = this;
    }

    // Note: this is called when any building is destroyed, not just "this task's" building
    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If the building which we have reserved a storage spot in was destroyed then try to find an alternative
        if (destroyedBuilding == Worker.StorageSpotReservedForItemInHand.Building)
        {
            Worker.StorageSpotReservedForItemInHand = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            substate = 0; // back to walking again
            if (Worker.StorageSpotReservedForItemInHand == null)
                Abandon();
        }
    }

    public override void OnBuildingMoved(BuildingData movedBuilding, LocationComponent previousLoc)
    {
        // If we're still walking, then determine if there is now a better/closer storage spot to deliver the item to. e.g. if user moved building far away
        // Note that we do this even if a building other than our target building moved, since a better alternative may have moved closer the worker.
        if (IsWalkingToTarget)
        {
            Worker.StorageSpotReservedForItemInHand = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            if (Worker.StorageSpotReservedForItemInHand == null)
                Debug.Assert(false, "Failed to find *any* spot to store in.  Shouldn't happen since we already had one reserved");
        }

        if (movedBuilding != Worker.AssignedBuilding) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += movedBuilding.Location - previousLoc;
        else
            Worker.Location += movedBuilding.Location - previousLoc;
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        // If the building we're delivering to is now paused then abort delivering the item to them and carry it somewhere else
        // This *should* look for a better need...
        if (building == Worker.StorageSpotReservedForItemInHand.Building)
        {
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(Worker.StorageSpotReservedForItemInHand, Worker.Location);
            if (newSpot == null)
                Abandon(); // Failed to find an alternative.  TODO: Test this; e.g. town storage is full, destroy building that last item is being delivered to.
            else
            {
                // Swap for new storage spot
                ReservedSpots.Remove(Worker.StorageSpotReservedForItemInHand);
                Worker.StorageSpotReservedForItemInHand = newSpot;
                ReservedSpots.Add(Worker.StorageSpotReservedForItemInHand);
            }
            return;
        }
        // Regardless of what building we're delivering to, if we're still moving then determine if there is now a better/closer storage spot to deliver the item to.
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
                if (MoveTowards(Worker.StorageSpotReservedForItemInHand.Location))
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