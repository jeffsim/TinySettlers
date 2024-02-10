using System;

[Serializable]
public class Task_PickupItemFromStorageSpot : BaseTask_TransportItemFromSpotToStorage
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    public Task_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) :
        base(worker, needData, spotWithItemToPickup, reservedSpotToStoreItemIn)
    {
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new Subtask_WalkToItemSpot(this, SpotWithItemToPickup));
        Subtasks.Add(new Subtask_PickupItemFromItemSpot(this, SpotWithItemToPickup));
        Subtasks.Add(new Subtask_WalkToItemSpot(this, ReservedSpotToStoreItemIn));
        Subtasks.Add(new Subtask_DropItemInItemSpot(this, ReservedSpotToStoreItemIn));
    }
}