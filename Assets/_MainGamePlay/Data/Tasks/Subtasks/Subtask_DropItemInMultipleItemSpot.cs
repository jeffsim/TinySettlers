using UnityEngine;

public class Subtask_DropItemInMultipleItemSpot : Subtask
{
    protected override float RunTime => 0.5f;
    [SerializeField] IContainerInBuilding ItemsSpot;
    public override ItemData GetTaskItem() => Task.Worker.Hands.FirstItem;

    public Subtask_DropItemInMultipleItemSpot(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask)
    {
        ItemsSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemsSpot.Building);
    }
    public override void Start()
    {
        Debug.Assert(Task.Worker.Hands.HasItem);
        base.Start();
    }

    public override void SubtaskComplete()
    {
        Task.Worker.DropItemInHandInSpot(ItemsSpot);
    }
}