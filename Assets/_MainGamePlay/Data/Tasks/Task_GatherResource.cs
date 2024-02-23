using System;
using UnityEngine;

[Serializable]
public class Task_GatherResource : Task
{
    public override string ToString() => $"Gather resource from {SpotToGatherFrom}";
    public override TaskType Type => TaskType.GatherResource;

    [SerializeField] public IItemSpotInBuilding SpotToGatherFrom;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    public bool IsWalkingToSpotToGatherFrom => SubtaskIndex == 0;
    public bool IsWalkingToSpotToDropItemIn => SubtaskIndex == 4;
    public bool IsDroppingItemInSpot => SubtaskIndex == 5;

    public Task_GatherResource(WorkerData worker, NeedData needData, IItemSpotInBuilding spotToGatherFrom, IItemSpotInBuilding spotToStoreItemIn) :
        base(worker, needData)
    {
        SpotToGatherFrom = ReserveSpotOnStart(spotToGatherFrom);
        SpotToStoreItemIn = ReserveSpotOnStart(spotToStoreItemIn);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToItemSpot(this, SpotToGatherFrom),
            1 => new Subtask_ReapItem(this, SpotToGatherFrom),
            2 => new Subtask_PickupItemFromItemSpot(this, SpotToGatherFrom),
            3 => new Subtask_UnreserveSpot(this, SpotToGatherFrom),
            4 => new Subtask_WalkToItemSpot(this, SpotToStoreItemIn),
            5 => new Subtask_DropItemInItemSpot(this, SpotToStoreItemIn),
            _ => null // No more subtasks
        };
    }

    public override void OnBuildingMoved(BuildingData building, Location previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        if (!IsDroppingItemInSpot)
        {
            SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotToDropItemIn ? Worker.Location : SpotToGatherFrom.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building);
            if (IsWalkingToSpotToDropItemIn)
                LastMoveToTarget.SetWorldLoc(SpotToStoreItemIn.Location);
        }
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        base.OnBuildingDestroyed(building);
        if (!IsRunning) return;

        if (SubtaskIndex < 4 && building == SpotToGatherFrom.Building)
            Abandon();
        else
        {
            // If building with storage spot was destroyed, find a new spot to store in
            if (building == SpotToStoreItemIn.Building)
            {
                UnreserveSpot(SpotToStoreItemIn);
                if ((SpotToStoreItemIn = FindAndReserveOptimalStorageSpot(IsWalkingToSpotToDropItemIn ? Worker.Location : SpotToGatherFrom.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building)) == null)
                    Abandon();
            }
        }
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        base.OnBuildingPauseToggled(building);
        if (!IsRunning) return;

        if (SubtaskIndex < 4 && building == SpotToGatherFrom.Building)
        {
            Abandon();
            return;
        }

        // Check if a better spot to store in is available
        if ((SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotToDropItemIn ? Worker.Location : SpotToGatherFrom.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building)) == null)
            Abandon();
    }

}