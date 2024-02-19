using UnityEngine;

public class Subtask_ReapItem : Subtask
{
    protected override float RunTime => 1;
    public override ItemData GetTaskItem() => ItemSpot.ItemContainer.Item;
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public Subtask_ReapItem(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }
}