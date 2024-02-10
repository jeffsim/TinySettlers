using UnityEngine;

public class Subtask_CraftItem : Subtask
{
    protected override float RunTime => 1;
    [SerializeField] IMultipleItemSpotInBuilding ItemsSpot;
    public override ItemDefn GetTaskItem() => GameDefns.Instance.ItemDefns[CraftingItemDefnId];
    public string CraftingItemDefnId;

    public Subtask_CraftItem(Task parentTask, string craftingItemDefnId, IMultipleItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemsSpot = itemSpot;
        CraftingItemDefnId = craftingItemDefnId;
        UpdateWorkerLocWhenBuildingMoves(ItemsSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Hands.SetItem(new ItemData() { DefnId = CraftingItemDefnId });
    }

    public override void OnAnyBuildingPauseToggled(BuildingData building)
    {
        if (building.IsPaused && building == ItemsSpot.Building)
            Task.Abandon();
    }

    public override void OnAnyBuildingDestroyed(BuildingData destroyedBuilding)
    {
        if (destroyedBuilding == ItemsSpot.Building)
            Task.Abandon();
    }
}