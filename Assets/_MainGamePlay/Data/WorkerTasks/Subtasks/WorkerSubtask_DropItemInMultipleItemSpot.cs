using UnityEngine;

public class WorkerSubtask_DropItemInMultipleItemSpot : WorkerSubtask
{
    protected override float RunTime => 0.5f;
    [SerializeField] IMultipleItemSpotInBuilding ItemSpot;
    public override ItemDefn GetTaskItem() => Task.Worker.Hands.Item.Defn;

    public WorkerSubtask_DropItemInMultipleItemSpot(WorkerTask parentTask, IMultipleItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
        UpdateWorkerLocWhenBuildingMoves(ItemSpot.Building);
    }
    public override void Start()
    {
        Debug.Assert(Task.Worker.Hands.HasItem);
        base.Start();
    }

    public override void SubtaskComplete()
    {
        Task.Worker.DropItemInHandInSpot(ItemSpot);
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