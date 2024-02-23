using UnityEngine;

public class Subtask_PickupItemFromItemSpot : Subtask
{
    protected override float RunTime => 0.5f;
    public override ItemData GetTaskItem() => ItemSpot.Container.FirstItem;
    [SerializeField] public IContainerInBuilding ItemSpot;

    public Subtask_PickupItemFromItemSpot(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        Debug.Assert(ItemSpot.Container.HasItem);
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Debug.Assert(ItemSpot.Container.HasItem);
        Task.Worker.Hands.AddItem(ItemSpot.Container.ClearItems());
        Debug.Assert(Task.Worker.Hands.HasItem);
    }
}