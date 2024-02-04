using UnityEngine;

public class WorkerSubtask_WalkToItemSpot : BaseWorkerSubtask_Moving
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

    public WorkerSubtask_WalkToItemSpot(WorkerTask parentTask, IItemSpotInBuilding itemSpot) : base(parentTask, itemSpot.Location)
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
