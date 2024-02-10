using UnityEngine;

public class Subtask_WalkToItemSpot : BaseSubtask_Moving
{
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public override ItemDefn GetTaskItem()
    {
        if (ItemSpot.ItemContainer.HasItem)
            return ItemSpot.ItemContainer.Item.Defn;
        if (Task.Worker.Hands.HasItem)
            return Task.Worker.Hands.Item.Defn;
        return null;
    }

    public Subtask_WalkToItemSpot(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask, itemSpot.Location)
    {
        ItemSpot = itemSpot;
        UpdateMoveTargetWhenBuildingMoves(ItemSpot.Building);
    }
}
