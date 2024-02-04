using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupItemFromStorageSpot : WorkerTask
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    [SerializeField] IItemSpotInBuilding SpotWithItemToPickup;
    [SerializeField] public IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public WorkerTask_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
    {
        SpotWithItemToPickup = ReserveSpotOnStart(spotWithItemToPickup);
        ReservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotWithItemToPickup));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotWithItemToPickup));
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        if (building == ReservedSpotToStoreItemIn.Building)
        {
            ReservedSpots.Remove(ReservedSpotToStoreItemIn);
            ReservedSpotToStoreItemIn = FindAndReserveNewOptimalStorageSpotToDeliverItemTo(ReservedSpotToStoreItemIn, SpotWithItemToPickup.Location);
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