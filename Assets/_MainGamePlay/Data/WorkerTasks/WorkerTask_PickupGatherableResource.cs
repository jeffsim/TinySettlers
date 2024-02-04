using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupGatherableResource : WorkerTask
{
    public override string ToString() => $"Pickup gatherable resource from {SpotToGatherFrom}";
    public override TaskType Type => TaskType.PickupGatherableResource;

    [SerializeField] public IItemSpotInBuilding SpotToGatherFrom;
    [SerializeField] public IItemSpotInBuilding SpotToStoreGatheredItemIn;

    public WorkerTask_PickupGatherableResource(WorkerData worker, NeedData needData, IItemSpotInBuilding gatheringSpot, IItemSpotInBuilding storageSpotToReserve) : base(worker, needData)
    {
        SpotToGatherFrom = ReserveSpotOnStart(gatheringSpot);
        SpotToStoreGatheredItemIn = ReserveSpotOnStart(storageSpotToReserve);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotToGatherFrom));
        Subtasks.Add(new WorkerSubtask_ReapGatherableResource(this, SpotToGatherFrom));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotToGatherFrom));
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = SpotToStoreGatheredItemIn;
        Worker.OriginalPickupItemNeed = Need;
        SpotToStoreGatheredItemIn.Reservation.ReserveBy(Worker);
    }
}