using UnityEngine;

public class Subtask_PickupItemFromItemSpot : Subtask
{
    protected override float RunTime => 0.5f;
    public override ItemDefn GetTaskItem() => ItemSpot.ItemContainer.Item.Defn;

    public Subtask_PickupItemFromItemSpot(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        Debug.Assert(ItemSpot.ItemContainer.HasItem);
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Debug.Assert(ItemSpot.ItemContainer.HasItem);
        Task.Worker.Hands.SetItem(ItemSpot.ItemContainer.ClearItem());
        Debug.Assert(Task.Worker.Hands.HasItem);
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