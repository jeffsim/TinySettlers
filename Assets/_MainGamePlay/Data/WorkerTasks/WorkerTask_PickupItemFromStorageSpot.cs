using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupItemFromStorageSpot : WorkerTask
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    [SerializeField] IItemSpotInBuilding SpotWithItemToPickup;
    [SerializeField] IItemSpotInBuilding ReservedSpotToStoreItemIn;

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

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = ReservedSpotToStoreItemIn;
        Worker.OriginalPickupItemNeed = Need;
        ReservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}