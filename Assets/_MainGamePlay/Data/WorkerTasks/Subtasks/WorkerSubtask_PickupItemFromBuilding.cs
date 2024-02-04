using UnityEngine;

public class WorkerSubtask_PickupItemFromBuilding : WorkerSubtask
{
    protected override float RunTime => 0.5f;
    [SerializeField] IItemSpotInBuilding ItemSpot;
    public override ItemDefn GetTaskItem() => ItemSpot.ItemContainer.Item.Defn;

    public WorkerSubtask_PickupItemFromBuilding(WorkerTask parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        Debug.Assert(ItemSpot.ItemContainer.HasItem);
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Debug.Assert(ItemSpot.ItemContainer.HasItem);
        Task.Worker.Hands.SetItem(ItemSpot.ItemContainer.ClearItem());
        Task.Worker.StorageSpotReservedForItemInHand = null; // TODO
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