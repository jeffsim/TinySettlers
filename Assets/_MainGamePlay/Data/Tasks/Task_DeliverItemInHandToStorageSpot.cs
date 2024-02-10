using System;

[Serializable]
public class Task_DeliverItemInHandToStorageSpot : NewBaseTask
{
    public override string ToString() => "Deliver item in hand to storage spot";
    public override TaskType Type => TaskType.DeliverItemInHandToStorageSpot;
    public IItemSpotInBuilding ReservedItemSpot;

    public Task_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need, IItemSpotInBuilding itemSpot) : base(worker, need)
    {
        ReservedItemSpot = itemSpot;
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToItemSpot(this, ReservedItemSpot),
            1 => new Subtask_DropItemInItemSpot(this, ReservedItemSpot),
            _ => null // No more subtasks
        };
    }
}