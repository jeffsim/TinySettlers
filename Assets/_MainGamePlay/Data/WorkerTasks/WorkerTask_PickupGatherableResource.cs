using System;

[Serializable]
public class WorkerTask_PickupGatherableResource : BaseWorkerTask_TransportItemFromSpotToStorage
{
    public override string ToString() => $"Pickup gatherable resource from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupGatherableResource;

    public WorkerTask_PickupGatherableResource(WorkerData worker, NeedData needData, IItemSpotInBuilding gatheringSpot, IItemSpotInBuilding reservedSpotToStoreItemIn) :
        base(worker, needData, gatheringSpot, reservedSpotToStoreItemIn)
    {
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotWithItemToPickup));
        Subtasks.Add(new WorkerSubtask_ReapGatherableResource(this, SpotWithItemToPickup));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotWithItemToPickup));
    }
}