using UnityEngine;

public class Subtask_PickupItemFromGround : Subtask
{
    protected override float RunTime => 0.5f;
    [SerializeReference] ItemData Item;
    public override ItemData GetTaskItem() => Item;

    public Subtask_PickupItemFromGround(Task parentTask, ItemData item) : base(parentTask)
    {
        Item = item;
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Town.RemoveItemFromGround(Item);
        Task.Worker.Hands.SetItem(Item);
    }
}