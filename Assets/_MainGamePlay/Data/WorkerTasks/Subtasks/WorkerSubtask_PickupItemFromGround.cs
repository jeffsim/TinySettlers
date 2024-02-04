using UnityEngine;

public class WorkerSubtask_PickupItemFromGround : WorkerSubtask
{
    protected override float RunTime => 0.5f;
    [SerializeReference] ItemData Item;
    public override ItemDefn GetTaskItem() => Item.Defn;

    public WorkerSubtask_PickupItemFromGround(WorkerTask parentTask, ItemData item) : base(parentTask)
    {
        Item = item;
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Town.RemoveItemFromGround(Item);
        Task.Worker.Hands.SetItem(Item);
    }
}