using System;
using UnityEngine;

[Serializable]
public class Task_TransportItemFromSpotToSpot : NewBaseTask
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.TransportItemFromSpotToSpot;

    [SerializeField] public IItemSpotInBuilding SpotWithItemToPickup;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    public bool IsWalkingToSpotToGatherFrom => SubtaskIndex == 0;
    public bool IsWalkingToSpotDropItemIn => SubtaskIndex == 2;

    public Task_TransportItemFromSpotToSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) :
        base(worker, needData)
    {
        SpotWithItemToPickup = ReserveSpotOnStart(spotWithItemToPickup);
        SpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToItemSpot(this, SpotWithItemToPickup),
            1 => new Subtask_PickupItemFromItemSpot(this, SpotWithItemToPickup),
            2 => new Subtask_UnreserveSpot(this, SpotWithItemToPickup),
            3 => new Subtask_WalkToItemSpot(this, SpotToStoreItemIn),
            4 => new Subtask_DropItemInItemSpot(this, SpotToStoreItemIn),
            _ => null // No more subtasks
        };
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

        if (SubtaskIndex < 3 && building == SpotWithItemToPickup.Building)
        {
            Abandon();
            return;
        }

        // Check if a better spot to store in is available
        var checkForBetterStorageSpot = !destroyed || building == SpotToStoreItemIn.Building;
        if (checkForBetterStorageSpot)
            if ((SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotDropItemIn ? Worker.Location : SpotWithItemToPickup.Location, IsWalkingToSpotDropItemIn && building == SpotToStoreItemIn.Building)) == null)
                Abandon();
    }
}