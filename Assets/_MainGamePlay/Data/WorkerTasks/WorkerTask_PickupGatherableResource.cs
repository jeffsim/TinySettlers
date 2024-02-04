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
        // Remove item from gathering spot and put it in Worker's hand, and we're done
        CompleteTask();
        Worker.Hands.SetItem(SpotToGatherFrom.ItemContainer.ClearItem());

        // NOTE that completing the task unreserved both the gathering spot and the storage spot so that others can use them.
        // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
        // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
        // for the reasons that I broke them apart in the first place...
        Worker.StorageSpotReservedForItemInHand = SpotToStoreGatheredItemIn;
        Worker.OriginalPickupItemNeed = Need;
        SpotToStoreGatheredItemIn.Reservation.ReserveBy(Worker);
    }
}