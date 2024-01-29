using System;
using UnityEngine;

public enum WorkerTask_PickupAbandonedItemFromGroundSubstate
{
    GotoItemOnGround = 0,
    PickupItemFromGround = 1,
};

[Serializable]
public class WorkerTask_PickupAbandonedItemFromGround : WorkerTask
{
    public override string ToString() => "Pickup item from ground";
    internal override string getDebuggerString() => $"Pickup item {ItemToPickup} from ground";

    public override TaskType Type => TaskType.PickupItemFromGround;
    [SerializeField] public ItemData ItemToPickup;

    [SerializeField] StorageSpotData reservedSpotToStoreItemIn;

    public const float secondsToPickup = 0.5f;

    public override bool IsWalkingToTarget => substate == 0;

    public override ItemDefn GetTaskItem() => ItemToPickup.Defn;

    public override string ToDebugString()
    {
        var str = "Pickup item from ground\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_PickupAbandonedItemFromGroundSubstate.GotoItemOnGround: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, ItemToPickup.WorldLocOnGround).ToString("0.0"); break;
            case (int)WorkerTask_PickupAbandonedItemFromGroundSubstate.PickupItemFromGround: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_PickupAbandonedItemFromGround Create(WorkerData worker, NeedData needData, StorageSpotData spotToReserve)
    {
        return new WorkerTask_PickupAbandonedItemFromGround(worker, needData, spotToReserve);
    }

    private WorkerTask_PickupAbandonedItemFromGround(WorkerData worker, NeedData needData, StorageSpotData reservedSpotToStoreItemIn) : base(worker, needData)
    {
        ItemToPickup = Need.AbandonedItemToPickup;

        // While this task is simply to go pick up an item, we wouldn't start the task if we didn't know that there was at least one place that we could bring the
        // resource to; we reserve that so that if no building needs it after we pick it up, we can still store it somewhere
        this.reservedSpotToStoreItemIn = reservedSpotToStoreItemIn;
    }

    public override void Start()
    {
        base.Start();

        // Now that we've actually started the task, we can reserve the spots that were passed in above.
        Need.AssignWorkerToMeetNeed(Worker);
        reserveStorageSpot(reservedSpotToStoreItemIn);
    }

    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        // If the building which we have reserved a storage spot in was destroyed then try to find an alternative
        if (destroyedBuilding == reservedSpotToStoreItemIn.Building)
        {
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(reservedSpotToStoreItemIn, Worker.WorldLoc);
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

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupAbandonedItemFromGroundSubstate.GotoItemOnGround: // go to resource spot
                if (MoveTowards(ItemToPickup.WorldLocOnGround, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_PickupAbandonedItemFromGroundSubstate.PickupItemFromGround: // gather in the building.
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    CompleteTask();
                    Worker.Town.RemoveItemFromGround(ItemToPickup);
                    Worker.AddItemToHands(ItemToPickup);

                    // NOTE that completing the task unreserved the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    Worker.StorageSpotReservedForItemInHand = reservedSpotToStoreItemIn;
                    Worker.OriginalPickupItemNeed = Need;
                    reservedSpotToStoreItemIn.ReserveBy(Worker);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}