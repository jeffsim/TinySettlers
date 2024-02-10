using System;
using UnityEngine;

[Serializable]
public class Task_TransportItemFromGroundToSpot : Task
{
    public override string ToString() => $"Pickup item {ItemToPickup} from ground";
    public override TaskType Type => TaskType.PickupItemFromGround;

    [SerializeField] public ItemData ItemToPickup;
    [SerializeField] IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public Task_TransportItemFromGroundToSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
    {
        ItemToPickup = Need.AbandonedItemToPickup;
        ReservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void Start()
    {
        base.Start();
        Need.AssignWorkerToMeetNeed(Worker);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToLocation(this, ItemToPickup.Location),
            1 => new Subtask_PickupItemFromGround(this, ItemToPickup),
            3 => new Subtask_WalkToItemSpot(this, ReservedSpotToStoreItemIn),
            4 => new Subtask_DropItemInItemSpot(this, ReservedSpotToStoreItemIn),
            _ => null // No more subtasks
        };
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.OriginalPickupItemNeed = Need;
    }
}