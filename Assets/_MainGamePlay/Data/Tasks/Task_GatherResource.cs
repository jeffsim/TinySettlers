using System;
using UnityEngine;

[Serializable]
public class Task_GatherResource : NewBaseTask
{
    public override string ToString() => $"Gather resource from {SpotToGatherFrom}";
    public override TaskType Type => TaskType.GatherResource;

    [SerializeField] public IItemSpotInBuilding SpotToGatherFrom;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    public bool IsWalkingToSpotToGatherFrom => SubtaskIndex == 0;
    public bool IsWalkingToSpotDropItemIn => SubtaskIndex == 4;
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

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        if (IsWalkingToSpotToGatherFrom)
            SpotToGatherFrom = FindAndReserveNewOptimalGatheringSpot(SpotToGatherFrom, Worker.Location, Need.NeededItem, true);

        if (!IsDroppingItemInSpot)
            SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotDropItemIn ? Worker.Location : SpotToGatherFrom.Location, IsWalkingToSpotDropItemIn && building == SpotToStoreItemIn.Building);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        base.OnBuildingDestroyed(building);
        HandleOnBuildingDestroyedOrPaused(building, true);
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        base.OnBuildingPauseToggled(building);
        HandleOnBuildingDestroyedOrPaused(building, false);
    }

    private void HandleOnBuildingDestroyedOrPaused(BuildingData building, bool destroyed)
    {
        if (!IsRunning) return;

        if (SubtaskIndex < 4 && building == SpotToGatherFrom.Building)
        {
            Abandon();
            return;
        }

        // Check if a better spot to store in is available
        var checkForBetterStorageSpot = !destroyed || building == SpotToStoreItemIn.Building;
        if (checkForBetterStorageSpot)
            if ((SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotDropItemIn ? Worker.Location : SpotToGatherFrom.Location, IsWalkingToSpotDropItemIn && building == SpotToStoreItemIn.Building)) == null)
                Abandon();
    }
}