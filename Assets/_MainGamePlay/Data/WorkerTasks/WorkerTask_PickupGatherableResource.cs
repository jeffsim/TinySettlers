using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupGatherableResource : WorkerTask
{
    public override string ToString() => $"Pickup gatherable resource from {SpotToGatherFrom}";
    public override TaskType Type => TaskType.PickupGatherableResource;

    [SerializeField] public IItemSpotInBuilding SpotToGatherFrom;
    [SerializeField] public IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public WorkerTask_PickupGatherableResource(WorkerData worker, NeedData needData, IItemSpotInBuilding gatheringSpot, IItemSpotInBuilding storageSpotToReserve) : base(worker, needData)
    {
        SpotToGatherFrom = ReserveSpotOnStart(gatheringSpot);
        ReservedSpotToStoreItemIn = ReserveSpotOnStart(storageSpotToReserve);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotToGatherFrom));
        Subtasks.Add(new WorkerSubtask_ReapGatherableResource(this, SpotToGatherFrom));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotToGatherFrom));
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        if (building == ReservedSpotToStoreItemIn.Building)
        {
            ReservedSpots.Remove(ReservedSpotToStoreItemIn);
            ReservedSpotToStoreItemIn = FindAndReserveNewOptimalStorageSpotToDeliverItemTo(ReservedSpotToStoreItemIn, SpotToGatherFrom.Location);
            if (ReservedSpotToStoreItemIn == null)
            {
                Abandon(); // failed to find a new spot to store the item in
                return;
            }
            ReservedSpots.Add(ReservedSpotToStoreItemIn);
        }
        base.OnBuildingPauseToggled(building);
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = ReservedSpotToStoreItemIn;
        Worker.OriginalPickupItemNeed = Need;
        ReservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}