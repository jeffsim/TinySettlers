using UnityEngine;

public class WorkerSubtask_CraftItem : WorkerSubtask
{
    protected override float RunTime => 1;
    [SerializeField] IMultipleItemSpotInBuilding ItemSpot;
    public override ItemDefn GetTaskItem() => Task.Worker.Hands.Item.Defn;
    public string CraftingItemDefnId;

    public WorkerSubtask_CraftItem(WorkerTask parentTask, string craftingItemDefnId, IMultipleItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        CraftingItemDefnId = craftingItemDefnId;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Hands.SetItem(new ItemData() { DefnId = CraftingItemDefnId });
    }

    public override void OnAnyBuildingPauseToggled(BuildingData building)
    {
        if (building.IsPaused && building == ItemSpot.Building)
            Task.Abandon();
    }

    public override void OnAnyBuildingDestroyed(BuildingData destroyedBuilding)
    {
        if (destroyedBuilding == ItemSpot.Building)
            Task.Abandon();
    }
}