using UnityEngine;

public class Subtask_WalkToMultipleItemSpot : BaseSubtask_Moving
{
    [SerializeField] public IMultipleItemSpotInBuilding ItemsSpot;

    public override ItemDefn GetTaskItem()
    {
        if (ItemsSpot.ItemsContainer.HasItem)
            return ItemsSpot.ItemsContainer.Items[0].Defn;
        if (Task.Worker.Hands.HasItem)
            return Task.Worker.Hands.Item.Defn;
        return null;
    }

    public Subtask_WalkToMultipleItemSpot(Task parentTask, IMultipleItemSpotInBuilding itemsSpot) : base(parentTask, itemsSpot.Location)
    {
        ItemsSpot = itemsSpot;
        UpdateMoveTargetWhenBuildingMoves(ItemsSpot.Building);
    }

    public override void OnAnyBuildingPauseToggled(BuildingData building)
    {
        if (building.IsPaused && building == ItemsSpot.Building)
            Task.Abandon();
    }

    public override void OnAnyBuildingDestroyed(BuildingData building)
    {
        if (building == ItemsSpot.Building)
            Task.Abandon();
    }
}
