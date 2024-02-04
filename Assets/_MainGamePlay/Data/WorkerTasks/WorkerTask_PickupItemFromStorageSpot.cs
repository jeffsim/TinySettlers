using System;

[Serializable]
public class WorkerTask_PickupItemFromStorageSpot : BaseWorkerTask_TransportItemFromSpotToStorage
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    public WorkerTask_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) :
        base(worker, needData, spotWithItemToPickup, reservedSpotToStoreItemIn)
    {
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotWithItemToPickup));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotWithItemToPickup));
    }
}