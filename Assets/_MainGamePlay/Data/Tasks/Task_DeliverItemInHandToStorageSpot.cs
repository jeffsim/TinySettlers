using System;

[Serializable]
public class Task_DeliverItemInHandToStorageSpot : Task
{
    public override string ToString() => "Deliver item in hand to storage spot";
    public override TaskType Type => TaskType.DeliverItemInHandToStorageSpot;
    public IItemSpotInBuilding ReservedItemSpot;

    public Task_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need, IItemSpotInBuilding itemSpot) : base(worker, need)
    {
        ReservedItemSpot = itemSpot;
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new Subtask_WalkToItemSpot(this, ReservedItemSpot));
        Subtasks.Add(new Subtask_DropItemInItemSpot(this, ReservedItemSpot));
    }
}