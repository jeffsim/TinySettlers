using System;
using UnityEngine;

[Serializable]
public class Task_GatherResource : Task
{
    public override string ToString() => $"Gather resource from {SpotToGatherFrom}";
    public override TaskType Type => TaskType.GetGatherableResource;

    [SerializeField] public IItemSpotInBuilding SpotToGatherFrom;
    [SerializeField] public IItemSpotInBuilding SpotToStoreItemIn;

    public Task_GatherResource(WorkerData worker, NeedData needData, IItemSpotInBuilding spotToGatherFrom, IItemSpotInBuilding spotToStoreItemIn) :
        base(worker, needData)
    {
        SpotToGatherFrom = ReserveSpotOnStart(spotToGatherFrom);
        SpotToStoreItemIn = ReserveSpotOnStart(spotToStoreItemIn);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new Subtask_WalkToItemSpot(this, SpotToGatherFrom));
        Subtasks.Add(new Subtask_ReapItem(this, SpotToGatherFrom));
        Subtasks.Add(new Subtask_PickupItemFromItemSpot(this, SpotToGatherFrom));
        Subtasks.Add(new Subtask_UnreserveSpot(this, SpotToGatherFrom)); //preemptively unreserve the spot so that others can use it
        Subtasks.Add(new Subtask_WalkToItemSpot(this, SpotToStoreItemIn));
        Subtasks.Add(new Subtask_DropItemInItemSpot(this, SpotToStoreItemIn));
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        switch (SubtaskIndex)
        {
            case 0: // Subtask_WalkToItemSpot
                SpotToGatherFrom = FindAndReserveNewOptimalGatheringSpot(SpotToGatherFrom, Worker.Location, Need.NeededItem, true);
                (Subtasks[3] as Subtask_UnreserveSpot).ItemSpot = SpotToGatherFrom;
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 1: // Subtask_ReapItem
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 2: // Subtask_PickupItemFromItemSpot
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 3: Debug.Assert(false, "Shouldn't hit this case"); break; // Subtask_UnreserveSpot
            case 4: // Subtask_WalkToItemSpot
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, true);
                break;
            case 5: break; // Subtask_DropItemInItemSpot -- do nothing               
        }
    }

    public override void OnBuildingDestroyed(BuildingData building) => HandleBuildingPausedOrDestroyed(building, true);
    public override void OnBuildingPauseToggled(BuildingData building) => HandleBuildingPausedOrDestroyed(building, false);

    void HandleBuildingPausedOrDestroyed(BuildingData building, bool destroyed)
    {
        if (building == SpotToStoreItemIn.Building)
        {
            ReservedSpots.Remove(SpotToStoreItemIn);
            SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpotOld(SpotToStoreItemIn, SpotToGatherFrom.Location);
            if (SpotToStoreItemIn == null)
            {
                Abandon(); // failed to find a new spot to store the item in
                return;
            }
            ReservedSpots.Add(SpotToStoreItemIn);
        }
        if (destroyed)
            base.OnBuildingDestroyed(building);
        else
            base.OnBuildingPauseToggled(building);
    }
}