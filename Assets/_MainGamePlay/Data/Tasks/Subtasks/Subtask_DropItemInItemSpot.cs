using UnityEngine;

public class Subtask_DropItemInItemSpot : Subtask
{
    protected override float RunTime => 0.5f;
    public override ItemData GetTaskItem() => Task.Worker.Hands.Item;
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public Subtask_DropItemInItemSpot(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Task.Worker.DropItemInHandInSpot(ItemSpot);
    }
}