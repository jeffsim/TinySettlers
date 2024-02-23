using System;

[Serializable]
public class Task_DeliverItemInHandToStorageSpot : Task
{
    public override string ToString() => "Deliver item in hand to storage spot";
    public override TaskType Type => TaskType.DeliverItemInHandToStorageSpot;
    public IItemSpotInBuilding SpotToStoreItemIn;

    public bool IsWalkingToSpotToDropItemIn => SubtaskIndex == 0;
    bool IsDroppingItemInSpot => SubtaskIndex == 1;

    public Task_DeliverItemInHandToStorageSpot(WorkerData worker, NeedData need, IItemSpotInBuilding itemSpot) : base(worker, need)
    {
        SpotToStoreItemIn = ReserveSpotOnStart(itemSpot);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToItemSpot(this, SpotToStoreItemIn),
            1 => new Subtask_DropItemInItemSpot(this, SpotToStoreItemIn),
            _ => null // No more subtasks
        };
    }

    public override void OnBuildingMoved(BuildingData building, Location previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        if (!IsDroppingItemInSpot)
        {
            SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, Worker.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building);
            if (IsWalkingToSpotToDropItemIn)
                LastMoveToTarget.SetWorldLoc(SpotToStoreItemIn.Location);
        }
    }
}