public class Subtask_WalkToItemSpot : BaseSubtask_Moving
{
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

    public override void OnAnyBuildingPauseToggled(BuildingData building)
    {
        if (building.IsPaused && building == ItemSpot.Building)
            Task.Abandon();
    }

    public override void OnAnyBuildingDestroyed(BuildingData building)
    {
        if (building == ItemSpot.Building)
            Task.Abandon();
    }
}