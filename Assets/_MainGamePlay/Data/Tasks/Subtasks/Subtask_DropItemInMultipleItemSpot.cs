using UnityEngine;

public class Subtask_DropItemInMultipleItemSpot : Subtask
{
    protected override float RunTime => 0.5f;
    [SerializeField] IMultipleItemSpotInBuilding ItemsSpot;
    public override ItemDefn GetTaskItem() => Task.Worker.Hands.Item.Defn;

    public Subtask_DropItemInMultipleItemSpot(Task parentTask, IMultipleItemSpotInBuilding itemSpot) : base(parentTask)
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