using UnityEngine;

public class WorkerSubtask_WalkToMultipleItemSpot : BaseWorkerSubtask_Moving
{
    [SerializeField] public IMultipleItemSpotInBuilding ItemSpot;

    public override ItemDefn GetTaskItem()
    {
        if (ItemSpot.ItemsContainer.HasItem)
            return ItemSpot.ItemsContainer.Items[0].Defn;
        if (Task.Worker.Hands.HasItem)
            return Task.Worker.Hands.Item.Defn;
        return null;
    }

    public WorkerSubtask_WalkToMultipleItemSpot(WorkerTask parentTask, IMultipleItemSpotInBuilding itemSpot) : base(parentTask, itemSpot.Location)
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
