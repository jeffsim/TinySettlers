using UnityEngine;

public class Subtask_ReapItem : Subtask
{
    protected override float RunTime => 1;
    public override ItemData GetTaskItem() => ItemSpot.Container.FirstItem;
    [SerializeField] public IContainerInBuilding ItemSpot;

    public Subtask_ReapItem(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }
}