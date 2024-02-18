using System;
using UnityEngine;

[Serializable]
public class Task_TransportItemFromSpotToSpot : Task
{
    public override string ToString() => $"Pickup item from {SpotWithItemToPickup}";
    public override TaskType Type => TaskType.TransportItemFromSpotToSpot;

    [SerializeField] public IItemSpotInBuilding SpotWithItemToPickup;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    bool IsWalkingToSpotWithItem => SubtaskIndex == 0;
    bool IsPickingUpItemInSpot => SubtaskIndex == 1;
    bool IsWalkingToSpotToDropItemIn => SubtaskIndex == 3;
    bool IsDroppingItemInSpot => SubtaskIndex == 4;
    bool IsWalking => SubtaskIndex == 0 || SubtaskIndex == 3;

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
        if (!IsRunning) return;

        if (SubtaskIndex < 3 && building == SpotWithItemToPickup.Building)
            Abandon();
        else
        {
            // If building with storage spot was destroyed, find a new spot to store in
            if (building == SpotToStoreItemIn.Building)
            {
                UnreserveSpot(SpotToStoreItemIn);
                if ((SpotToStoreItemIn = FindAndReserveOptimalStorageSpot(IsWalkingToSpotToDropItemIn ? Worker.Location : SpotWithItemToPickup.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building)) == null)
                    Abandon();
            }
        } 
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        base.OnBuildingPauseToggled(building);
        if (!IsRunning) return;

        if (SubtaskIndex < 3 && building == SpotWithItemToPickup.Building)
        {
            Abandon();
            return;
        }

        // Check if a better spot to store in is available
        if ((SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotToDropItemIn ? Worker.Location : SpotWithItemToPickup.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building)) == null)
            Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        if (!IsDroppingItemInSpot)
        {
            SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotToDropItemIn ? Worker.Location : SpotWithItemToPickup.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building);
            if (IsWalkingToSpotToDropItemIn)
                LastMoveToTarget.SetWorldLoc(SpotToStoreItemIn.Location);
        }
    }
}