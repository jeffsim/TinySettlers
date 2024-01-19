using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorkerTask_GatherResourceSubstate
{
    GotoResourceBuilding = 0,
    GatherResourceInBuilding = 1,
    ReturnToAssignedBuilding = 2,
    DropGatheredResource = 3
};

[Serializable]
public class WorkerTask_GatherResource : WorkerTask
{
    public override string ToString() => "Gather resource";

    public override TaskType Type => TaskType.GatherResource;

    [SerializeField] BuildingData buildingGatheringFrom;
    [SerializeField] string GatheringItemDefnId;
    [SerializeField] float CarryingSpeedMultiplier;
    [SerializeField] StorageSpotData reservedStorageSpot;
    [SerializeField] GatheringSpotData reservedGatheringSpot;

    public const float secondsToGather = 1;
    public const float secondsToDrop = 0.5f;

    public override bool Debug_IsMovingToTarget => substate == 0 || substate == 2;

    public override ItemDefn GetTaskItem()
    {
        if (string.IsNullOrEmpty(GatheringItemDefnId)) return null;
        return GameDefns.Instance.ItemDefns[GatheringItemDefnId];
    }

    public override string ToDebugString()
    {
        var str = "Gather\n";
        str += "  Item: " + GatheringItemDefnId + "\n";
        str += "  Gather from: " + reservedGatheringSpot.InstanceId + " (" + buildingGatheringFrom.DefnId + "), gatherspot: " + reservedGatheringSpot.InstanceId + "\n";
        str += "  TargetStorage: " + reservedStorageSpot.InstanceId + " (" + reservedStorageSpot.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_GatherResourceSubstate.GotoResourceBuilding: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, reservedGatheringSpot.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding: str += "; per = " + getPercentSubstateDone(secondsToGather); break;
            case (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, reservedStorageSpot.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_GatherResourceSubstate.DropGatheredResource: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public override bool IsCarryingItem(string itemId)
    {
        return substate > 1 && GatheringItemDefnId == itemId;
    }

    // TODO: Pooling
    public static WorkerTask_GatherResource Create(WorkerData worker, ItemDefn itemToGather, BuildingData buildingToGatherFrom)
    {
        return new WorkerTask_GatherResource(worker, itemToGather, buildingToGatherFrom);
    }

    private WorkerTask_GatherResource(WorkerData worker, ItemDefn itemToGather, BuildingData buildingToGatherFrom) : base(worker)
    {
        buildingGatheringFrom = buildingToGatherFrom;
        GatheringItemDefnId = itemToGather.Id;
        CarryingSpeedMultiplier = itemToGather.CarryingSpeedModifier;
    }

    public override void Start()
    {
        base.Start();
        reservedStorageSpot = reserveStorageSpot(Worker.AssignedBuilding);
        reservedGatheringSpot = reserveBuildingGatheringSpot(buildingGatheringFrom);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // If our target resource-gathering building was destroyed and we're still walking to it, or gathering from it, then abandon
        if (buildingGatheringFrom.IsDestroyed && substate < 2)
            Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_GatherResourceSubstate)substate)
        {
            case WorkerTask_GatherResourceSubstate.GotoResourceBuilding: updateMoveToLoc = building == buildingGatheringFrom; break;
            case WorkerTask_GatherResourceSubstate.GatherResourceInBuilding: updateWorkerLoc = building == buildingGatheringFrom; break;
            case WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding: updateMoveToLoc = building == Worker.AssignedBuilding; break;
            case WorkerTask_GatherResourceSubstate.DropGatheredResource: updateWorkerLoc = building == Worker.AssignedBuilding; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_GatherResourceSubstate.GotoResourceBuilding: // go to resource source building
                if (moveTowards(reservedGatheringSpot.WorldLoc, distanceMovedPerSecond))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_GatherResourceSubstate.GatherResourceInBuilding: // gather in the building.
                if (getPercentSubstateDone(secondsToGather) == 1)
                {
                    // Done gathering; let someone else gather from it and goto next substate
                    unreserveBuildingGatheringSpot(reservedGatheringSpot);

                    // We've already reserved a storage spot for the crafted item, but other stored items may have changed since we reserved the spot.
                    if (betterStorageSpotExists(reservedStorageSpot))
                        reservedStorageSpot = getBetterStorageSpot(reservedStorageSpot);

                    gotoNextSubstate();
                }
                break;

            case (int)WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding: // Walk back to our assigned building
                if (moveTowards(reservedStorageSpot.WorldLoc, distanceMovedPerSecond * CarryingSpeedMultiplier))
                    gotoNextSubstate();
                break;

            case (int)WorkerTask_GatherResourceSubstate.DropGatheredResource: // drop the gathered item; and then done.
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // Done dropping.  Add the item into the storage spot.  Complete the task first so that the spot is unreserved so that we can add to it
                    CompleteTask();
                    Worker.AssignedBuilding.AddItemToStorage(new ItemData() { DefnId = GatheringItemDefnId });
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}