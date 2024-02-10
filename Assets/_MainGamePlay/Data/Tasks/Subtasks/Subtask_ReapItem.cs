using UnityEngine;

public class Subtask_ReapItem : Subtask
{
    protected override float RunTime => 1;
    public override ItemDefn GetTaskItem() => ItemSpot.ItemContainer.Item.Defn;
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public Subtask_ReapItem(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }
}