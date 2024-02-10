using UnityEngine;

public class Subtask_SellItemInHands : Subtask
{
    protected override float RunTime => 1;
    [SerializeField] IItemSpotInBuilding ItemSpot;
    public override ItemDefn GetTaskItem() => Task.Worker.Hands.Item.Defn;

    public Subtask_SellItemInHands(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }

    public override void SubtaskComplete()
    {
        Task.Worker.Town.ItemSold(Task.Worker.Hands.ClearItem());
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