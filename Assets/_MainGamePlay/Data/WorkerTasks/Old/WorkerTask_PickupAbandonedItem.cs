using System;
using UnityEngine;

public enum WorkerTask_PickupAbandonedItemSubstate
{
    GotoItemOnGround = 0,
    PickupItemOnGround = 1,
    GotoDestinationBuilding = 2,
    DropItemInBuilding = 3
};

[Serializable]
public class WorkerTask_PickupAbandonedItem : WorkerTask
{
    public override string ToString() => "Pick up abandoned item";
    public override TaskType Type => TaskType.PickupAbandonedItem;

    [SerializeField] StorageSpotData destinationStorageSpotForItem;
    [SerializeField] public ItemData ItemToPickup;
    [SerializeField] float CarryingSpeedMultiplier;

    public const float secondsToPickup = 1;
    public const float secondsToDrop = 0.5f;

    public override bool Debug_IsMovingToTarget => substate == 0 || substate == 2;

    public override ItemDefn GetTaskItem() => ItemToPickup?.Defn;

    public override string ToDebugString()
    {
        var str = "Ferry\n";
        str += "  Item: " + ItemToPickup.DefnId + "\n";
        str += "  TargetStorage: " + destinationStorageSpotForItem.InstanceId + " (" + destinationStorageSpotForItem.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_PickupAbandonedItemSubstate.GotoItemOnGround: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, ItemToPickup.WorldLocOnGround).ToString("0.0"); break;
            case (int)WorkerTask_PickupAbandonedItemSubstate.PickupItemOnGround: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            case (int)WorkerTask_PickupAbandonedItemSubstate.GotoDestinationBuilding: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, destinationStorageSpotForItem.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_PickupAbandonedItemSubstate.DropItemInBuilding: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public override bool IsCarryingItem(string itemId)
    {
        return substate > 2 && ItemToPickup.DefnId == itemId;
    }

    // TODO: Pooling
    public static WorkerTask_PickupAbandonedItem Create(WorkerData worker, NeedData need)
    {
        return new WorkerTask_PickupAbandonedItem(worker, need);
    }

    private WorkerTask_PickupAbandonedItem(WorkerData worker, NeedData need) : base(worker, need)
    {
        Need = need;
        ItemToPickup = need.AbandonedItemToPickup;
        CarryingSpeedMultiplier = ItemToPickup.Defn.CarryingSpeedModifier;
    }

    public override void Start()
    {
        base.Start();
        Need.AssignWorkerToMeetNeed(Worker);
        destinationStorageSpotForItem = reserveStorageSpotClosestToWorldLoc_AssignedBuildingOrPrimaryStorageOnly(Worker.WorldLoc);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // If the building to which we are bringing the item is destroyed then abandon
        // TODO-MUST: What happens to the ferried item when the task is abandoned while carrying it?
        if (destinationStorageSpotForItem.Building.IsDestroyed && substate < 4)
        {
            Need.UnassignWorkerToMeetNeed(Worker);
            Abandon();
        }
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_PickupAbandonedItemSubstate)substate)
        {
            case WorkerTask_PickupAbandonedItemSubstate.GotoItemOnGround: break;
            case WorkerTask_PickupAbandonedItemSubstate.PickupItemOnGround: break;
            case WorkerTask_PickupAbandonedItemSubstate.GotoDestinationBuilding: updateMoveToLoc = building == destinationStorageSpotForItem.Building; break;
            case WorkerTask_PickupAbandonedItemSubstate.DropItemInBuilding: updateWorkerLoc = building == destinationStorageSpotForItem.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupAbandonedItemSubstate.GotoItemOnGround: // go to item on ground
                if (moveTowards(ItemToPickup.WorldLocOnGround, distanceMovedPerSecond))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_PickupAbandonedItemSubstate.PickupItemOnGround: // pick up item
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    // We've already reserved a storage spot for the crafted item, but other stored items may have changed since we reserved the spot.
                    destinationStorageSpotForItem = getBetterStorageSpotThanSpotIfExists(destinationStorageSpotForItem);

                    // remove item from ground
                    Worker.Town.RemoveItemFromGround(ItemToPickup);

                    gotoNextSubstate();
                }
                break;

            case (int)WorkerTask_PickupAbandonedItemSubstate.GotoDestinationBuilding: // Walk back to our assigned building
                if (moveTowards(destinationStorageSpotForItem.WorldLoc, distanceMovedPerSecond * CarryingSpeedMultiplier))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_PickupAbandonedItemSubstate.DropItemInBuilding: // drop the gathered item; and then done.
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // Done dropping.  Add the item into the storage spot.  Complete the task first so that the spot is unreserved so that we can add to it
                    CompleteTask();
                    destinationStorageSpotForItem.Building.AddItemToItemSpot(ItemToPickup, destinationStorageSpotForItem);
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