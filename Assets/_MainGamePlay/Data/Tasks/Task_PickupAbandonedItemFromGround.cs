using System;
using UnityEngine;

[Serializable]
public class Task_PickupAbandonedItemFromGround : Task
{
    public override string ToString() => $"Pickup item {ItemToPickup} from ground";
    public override TaskType Type => TaskType.PickupItemFromGround;

    [SerializeField] public ItemData ItemToPickup;
    [SerializeField] IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public Task_PickupAbandonedItemFromGround(WorkerData worker, NeedData needData, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
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
        Subtasks.Add(new Subtask_WalkToLocation(this, ItemToPickup.Location));
        Subtasks.Add(new Subtask_PickupItemFromGround(this, ItemToPickup));
        Subtasks.Add(new Subtask_WalkToItemSpot(this, ReservedSpotToStoreItemIn));
        Subtasks.Add(new Subtask_DropItemInItemSpot(this, ReservedSpotToStoreItemIn));
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.OriginalPickupItemNeed = Need;
        ReservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}