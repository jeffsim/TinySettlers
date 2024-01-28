using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorkerTask_FerryItemSubstate
{
    // TODO: What about ferry from ground (no building)?
    GotoBuildingWithItem = 0,
    PickupItemInBuilding = 1,
    GotoDestinationBuilding = 2,
    DropItemInBuilding = 3
};

[Serializable]
public class WorkerTask_FerryItem : WorkerTask
{
    public override string ToString() => "Ferry item";
    public override TaskType Type => TaskType.FerryItem;

    [SerializeField] StorageSpotData storageSpotWithItem;
    [SerializeField] StorageSpotData destinationStorageSpotForItem;
    [SerializeField] public ItemData itemBeingFerried;
    [SerializeField] public BuildingData destBuilding;
    [SerializeField] float CarryingSpeedMultiplier;
    public const float secondsToPickup = 1;
    public const float secondsToDrop = 0.5f;

    public override bool IsWalkingToTarget => substate == 0 || substate == 2;

    public override ItemDefn GetTaskItem()
    {
        if (itemBeingFerried == null) return null;
        return itemBeingFerried.Defn;
    }

    internal override string getDebuggerString()
    {
        return $"Ferry {itemBeingFerried} from {storageSpotWithItem.Building} to {destinationStorageSpotForItem.Building}";
    }

    public override string ToDebugString()
    {
        var str = "Ferry\n";
        str += "  Item: " + itemBeingFerried.DefnId + "\n";
        str += "  SourceStorage: " + storageSpotWithItem.InstanceId + " (" + storageSpotWithItem.Building.DefnId + ")\n";
        str += "  TargetStorage: " + destinationStorageSpotForItem.InstanceId + " (" + destinationStorageSpotForItem.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_FerryItemSubstate.GotoBuildingWithItem: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, storageSpotWithItem.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_FerryItemSubstate.PickupItemInBuilding: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            case (int)WorkerTask_FerryItemSubstate.GotoDestinationBuilding: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, destinationStorageSpotForItem.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_FerryItemSubstate.DropItemInBuilding: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public override bool IsCarryingItem(string itemId)
    {
        return substate > 1 && itemBeingFerried.DefnId == itemId;
    }

    // TODO: Pooling
    public static WorkerTask_FerryItem Create(WorkerData worker, StorageSpotData storageSpotWithItem, BuildingData destBuilding)
    {
        return new WorkerTask_FerryItem(worker, storageSpotWithItem, destBuilding);
    }

    private WorkerTask_FerryItem(WorkerData worker, StorageSpotData storageSpotWithItem, BuildingData destBuilding) : base(worker)
    {
        this.storageSpotWithItem = storageSpotWithItem;
        this.destBuilding = destBuilding;
        CarryingSpeedMultiplier = storageSpotWithItem.ItemInSpot.Defn.CarryingSpeedModifier;
        itemBeingFerried = storageSpotWithItem.ItemInSpot;
    }

    public override void Start()
    {
        base.Start();
        reserveStorageSpot(storageSpotWithItem);
        if (destBuilding == null)
        {
            // Need doesn't care where it goes - e.g. StorageCleanup.  Find the closest primary storage spot that can store the item
            destinationStorageSpotForItem = reserveStorageSpot(Worker.Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Primary, storageSpotWithItem.WorldLoc));
        }
        else
        {
            // Need specified a destination building - e.g. Crafting.  Find the closest empty storage spot in that building
            destinationStorageSpotForItem = reserveStorageSpot(destBuilding.GetClosestEmptyStorageSpot(storageSpotWithItem.WorldLoc));
        }
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // If the building with the item is destroyed and we have not yet picked it up, then abandon
        if (storageSpotWithItem.Building.IsDestroyed && substate < 2)
            Abandon();

        // If the building to which we are bringing the item is destroyed then abandon
        // TODO-MUST: What happens to the ferried item when the task is abandoned while carrying it?
        if (destinationStorageSpotForItem.Building.IsDestroyed && substate < 4)
            Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_FerryItemSubstate)substate)
        {
            case WorkerTask_FerryItemSubstate.GotoBuildingWithItem: updateMoveToLoc = building == storageSpotWithItem.Building; break;
            case WorkerTask_FerryItemSubstate.PickupItemInBuilding: updateWorkerLoc = building == storageSpotWithItem.Building; break;
            case WorkerTask_FerryItemSubstate.GotoDestinationBuilding: updateMoveToLoc = building == destinationStorageSpotForItem.Building; break;
            case WorkerTask_FerryItemSubstate.DropItemInBuilding: updateWorkerLoc = building == destinationStorageSpotForItem.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_FerryItemSubstate.GotoBuildingWithItem: // go to resource source building
                if (MoveTowards(storageSpotWithItem.WorldLoc, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_FerryItemSubstate.PickupItemInBuilding: // gather in the building.
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    // Done picking up; unreserve the storage spot so someone else can use it
                    unreserveStorageSpot(storageSpotWithItem);

                    storageSpotWithItem.RemoveItem();

                    // We've already reserved a storage spot for the crafted item, but other stored items may have changed since we reserved the spot.
                    destinationStorageSpotForItem = getBetterStorageSpotThanSpotIfExists(destinationStorageSpotForItem);
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_FerryItemSubstate.GotoDestinationBuilding: // Walk back to our assigned building
                if (MoveTowards(destinationStorageSpotForItem.WorldLoc, distanceMovedPerSecond * CarryingSpeedMultiplier))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_FerryItemSubstate.DropItemInBuilding: // drop the gathered item; and then done.
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // Done dropping.  Add the item into the storage spot.  Complete the task first so that the spot is unreserved so that we can add to it
                    CompleteTask();
                    destinationStorageSpotForItem.Building.AddItemToItemSpot(itemBeingFerried, destinationStorageSpotForItem);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}