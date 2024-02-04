using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupItemFromStorageSpot : WorkerTask
{
    public override string ToString() => $"Pickup item from {spotWithItemToPickup}";
    public override TaskType Type => TaskType.PickupItemInStorageSpot;

    [SerializeField] IItemSpotInBuilding spotWithItemToPickup;
    [SerializeField] IItemSpotInBuilding reservedSpotToStoreItemIn;

    public WorkerTask_PickupItemFromStorageSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
    {
        this.spotWithItemToPickup = ReserveSpotOnStart(spotWithItemToPickup);
        this.reservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, spotWithItemToPickup));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, spotWithItemToPickup));
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = reservedSpotToStoreItemIn;
        Worker.OriginalPickupItemNeed = Need;
        reservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}