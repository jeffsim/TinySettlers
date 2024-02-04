using UnityEngine;

public class WorkerSubtask_ReapGatherableResource : WorkerSubtask
{
    protected override float RunTime => 1;
    [SerializeField] IItemSpotInBuilding ItemSpot;
    public override ItemDefn GetTaskItem() => ItemSpot.ItemContainer.Item.Defn;

    public WorkerSubtask_ReapGatherableResource(WorkerTask parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
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