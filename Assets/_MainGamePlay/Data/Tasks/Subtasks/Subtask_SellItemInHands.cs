using UnityEngine;

public class Subtask_SellItemInHands : Subtask
{
    protected override float RunTime => 1;
    public override ItemData GetTaskItem() => Task.Worker.Hands.FirstItem;
    [SerializeField] public IContainerInBuilding ItemSpot;

    public Subtask_SellItemInHands(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Town.ItemSold(Task.Worker.Hands.ClearItems());
    }
}