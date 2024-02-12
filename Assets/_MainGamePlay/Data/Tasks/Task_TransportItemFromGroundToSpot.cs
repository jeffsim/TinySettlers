using System;
using UnityEngine;

[Serializable]
public class Task_TransportItemFromGroundToSpot : Task
{
    public override string ToString() => $"Pickup item {ItemToPickup} from ground";
    public override TaskType Type => TaskType.PickupItemFromGround;

    [SerializeField] public ItemData ItemToPickup;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    public bool IsWalkingToItemOnGround => SubtaskIndex == 0;
    public bool IsWalkingToSpotToDropItemIn => SubtaskIndex == 2;

    public Task_TransportItemFromGroundToSpot(WorkerData worker, NeedData needData, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, needData)
    {
        ItemToPickup = Need.AbandonedItemToPickup;
        SpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void Start()
    {
        base.Start();
        Need.AssignWorkerToMeetNeed(Worker);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToLocation(this, ItemToPickup.Location),
            1 => new Subtask_PickupItemFromGround(this, ItemToPickup),
            3 => new Subtask_WalkToItemSpot(this, SpotToStoreItemIn),
            4 => new Subtask_DropItemInItemSpot(this, SpotToStoreItemIn),
            _ => null // No more subtasks
        };
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.OriginalPickupItemNeed = Need;
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        base.OnBuildingDestroyed(building);
        if (IsRunning) HandleOnBuildingDestroyedOrPaused(building, true);
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        base.OnBuildingPauseToggled(building);
        if (IsRunning) HandleOnBuildingDestroyedOrPaused(building, false);
    }

    private void HandleOnBuildingDestroyedOrPaused(BuildingData building, bool destroyed)
    {
        // Check if a better spot to store in is available
        if (!destroyed || building == SpotToStoreItemIn.Building)
            if ((SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, IsWalkingToSpotToDropItemIn ? Worker.Location : ItemToPickup.Location, IsWalkingToSpotToDropItemIn && building == SpotToStoreItemIn.Building)) == null)
                Abandon();
    }
}