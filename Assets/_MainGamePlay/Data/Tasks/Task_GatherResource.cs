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
        Subtasks.Add(new Subtask_WalkToItemSpot(this, null));
        Subtasks.Add(new Subtask_ReapItem(this, null));
        Subtasks.Add(new Subtask_PickupItemFromItemSpot(this, null));
        Subtasks.Add(new Subtask_UnreserveSpot(this));
        Subtasks.Add(new Subtask_WalkToItemSpot(this, null));
        Subtasks.Add(new Subtask_DropItemInItemSpot(this, null));
    }

    public override void OnSubtaskStart()
    {
        switch (SubtaskIndex)
        {
            case 0: CurSubTask.ItemSpot = SpotToGatherFrom; break;
            case 1: CurSubTask.ItemSpot = SpotToGatherFrom; break;
            case 2: CurSubTask.ItemSpot = SpotToGatherFrom; break;
            case 3: CurSubTask.ItemSpot = SpotToGatherFrom; break;
            case 4: CurSubTask.ItemSpot = SpotToStoreItemIn; break;
            case 5: CurSubTask.ItemSpot = SpotToStoreItemIn; break;
        }
    }
    
    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        base.OnBuildingMoved(building, previousLoc);

        switch (SubtaskIndex)
        {
            case 0: // Subtask_WalkToItemSpot
                SpotToGatherFrom = FindAndReserveNewOptimalGatheringSpot(SpotToGatherFrom, Worker.Location, Need.NeededItem, true);
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 1: // Subtask_ReapItem
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 2: // Subtask_PickupItemFromItemSpot
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, false);
                break;
            case 4: // Subtask_WalkToItemSpot
                SpotToStoreItemIn = FindAndReserveNewOptimalStorageSpot(SpotToStoreItemIn, SpotToGatherFrom.Location, true);
                break;
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