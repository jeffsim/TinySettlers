using System;

[Serializable]
public class WorkerTask_DeliverItemInHandToStorageSpot : WorkerTask
{
    public override string ToString() => "Deliver item in hand to storage spot";
    public override TaskType Type => TaskType.DeliverItemInHandToStorageSpot;

    public WorkerTask_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need) : base(worker, need)
    {
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, Worker.StorageSpotReservedForItemInHand));
        Subtasks.Add(new WorkerSubtask_DropItemInItemSpot(this, Worker.StorageSpotReservedForItemInHand));
    }
}