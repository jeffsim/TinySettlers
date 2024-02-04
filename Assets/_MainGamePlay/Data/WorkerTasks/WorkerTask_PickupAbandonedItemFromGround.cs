using System;
using UnityEngine;

[Serializable]
public class WorkerTask_PickupAbandonedItemFromGround : WorkerTask
{
    public override string ToString() => $"Pickup item {ItemToPickup} from ground";
    public override TaskType Type => TaskType.PickupItemFromGround;

    [SerializeField] public ItemData ItemToPickup;
    [SerializeField] IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public WorkerTask_PickupAbandonedItemFromGround(WorkerData worker, NeedData needData, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
    {
        ItemToPickup = Need.AbandonedItemToPickup;
        ReservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void Start()
    {
        base.Start();
        Need.AssignWorkerToMeetNeed(Worker);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToLocation(this, ItemToPickup.Location));
        Subtasks.Add(new WorkerSubtask_PickupItemFromGround(this, ItemToPickup));
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = ReservedSpotToStoreItemIn;
        Worker.OriginalPickupItemNeed = Need;
        ReservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}