using System;
using System.Collections.Generic;
using UnityEngine;

public enum TaskType { Unset, Idle, GatherResource, CraftGood, FerryItem, PickupAbandonedItem };

public enum TaskState { Unset, NotStarted, Started, Completed, Abandoned };

[Serializable]
public abstract class WorkerTask
{
    public virtual TaskType Type => TaskType.Unset;
    public TaskState TaskState = TaskState.Unset;

    public bool IsRunning => TaskState == TaskState.Started;

    public virtual bool Debug_IsMovingToTarget => false;

    [SerializeReference] public WorkerData Worker;

    public float timeStartedSubstate;
    public int substate;

    public Vector3 LastMoveToTarget;

    [SerializeField] List<GatheringSpotData> ReservedGatheringSpots;
    [SerializeField] List<StorageSpotData> ReservedStorageSpots;

    public bool HasReservedStorageSpot(StorageSpotData spot) => ReservedStorageSpots.Contains(spot);
    public bool HasReservedCraftingSpot(CraftingSpotData spot) => ReservedCraftingSpots.Contains(spot);
    public bool HasReservedGatheringSpot(GatheringSpotData spot) => ReservedGatheringSpots.Contains(spot);

    public abstract string ToDebugString();

    public virtual bool IsCarryingItem(string itemId) => false;

    [SerializeField] List<StorageSpotData> ReservedCraftingResourceStorageSpots;
    [SerializeField] List<CraftingSpotData> ReservedCraftingSpots;

    [SerializeField] protected float distanceMovedPerSecond = 5;

    public virtual ItemDefn GetTaskItem()
    {
        Debug.Assert(false, "GetTaskItem not implemented for task type");
        return null;
    }

    protected WorkerTask(WorkerData workerData)
    {
        Worker = workerData;
        ReservedGatheringSpots = new List<GatheringSpotData>();
        ReservedStorageSpots = new List<StorageSpotData>();
        ReservedCraftingSpots = new List<CraftingSpotData>();
        ReservedCraftingResourceStorageSpots = new List<StorageSpotData>();
    }

    public virtual void Start()
    {
        TaskState = TaskState.Started;
        substate = 0;
    }

    public virtual void Update()
    {
        Debug.Assert(IsRunning, "Updating nonrunning task (state = " + TaskState + ")");
        Debug.Assert(Worker.AssignedBuilding != null, "Failed to cancel task when assigned building cleared");
    }

    protected virtual void CompleteTask()
    {
        Cleanup();
        TaskState = TaskState.Completed;
        Worker.OnTaskCompleted(false);
    }

    public virtual void Abandon()
    {
        Cleanup();
        TaskState = TaskState.Abandoned;
        Worker.OnTaskCompleted(true);
    }

    protected void gotoSubstate(int num)
    {
        substate = num;
        timeStartedSubstate = GameTime.time;
        Update();
    }

    protected void gotoNextSubstate() => gotoSubstate(substate + 1);


    public float getPercentSubstateDone(float substateRuntime)
    {
        // started at 12, run for 5 seconds.  done at 17.  current = 12,14.5,17
        // (current-start) / total
        return Math.Clamp((GameTime.time - timeStartedSubstate) / (substateRuntime / GameTime.timeScale), 0, 1);
        // if (GameTime.time > timeStartedSubstate + substateRuntime / GameTime.timeScale)
    }

    // ==== GATHERING ===================================================

    protected GatheringSpotData reserveBuildingGatheringSpot(BuildingData buildingGatheringFrom)
    {
        var spot = buildingGatheringFrom.ReserveGatheringSpot(Worker);
        Debug.Assert(spot != null, "Failed to reserve gathering spot in " + buildingGatheringFrom.DefnId);
        ReservedGatheringSpots.Add(spot);
        return spot;
    }

    protected void unreserveBuildingGatheringSpot(GatheringSpotData spot)
    {
        spot.Unreserve();
        ReservedGatheringSpots.Remove(spot);
    }


    // ==== STORAGE ===================================================

    protected StorageSpotData reserveStorageSpot(BuildingData buildingToStoreIn)
    {
        var spot = buildingToStoreIn.ReserveStorageSpot(Worker);
        Debug.Assert(spot != null, "Failed to reserve storage spot in " + buildingToStoreIn.DefnId);
        ReservedStorageSpots.Add(spot);
        return spot;
    }

    protected StorageSpotData reserveStorageSpot(StorageSpotData spot)
    {
        spot.ReserveBy(Worker);
        ReservedStorageSpots.Add(spot);
        return spot;
    }

    protected void unreserveStorageSpot(StorageSpotData spot)
    {
        spot.Unreserve();
        ReservedStorageSpots.Remove(spot);
    }


    // ==== CRAFTING ===================================================

    protected CraftingSpotData reserveBuildingCraftingSpot(BuildingData buildingToCraftIn)
    {
        var spot = buildingToCraftIn.ReserveCraftingSpot(Worker);
        Debug.Assert(spot != null, "Failed to reserve crafting spot in " + buildingToCraftIn.DefnId);
        ReservedCraftingSpots.Add(spot);
        return spot;
    }

    protected void unreserveBuildingCraftingSpot(CraftingSpotData spot)
    {
        spot.Unreserve();
        ReservedCraftingSpots.Remove(spot);
    }


    // ==== CRAFTING RESOURCES =========================================

    protected bool HasMoreCraftingResourcesToGet()
    {
        return ReservedCraftingResourceStorageSpots.Count > 0;
    }

    protected void reserveCraftingResourceStorageSpotForItem(ItemDefn itemDefn)
    {
        var spot = Worker.AssignedBuilding.GetStorageSpotWithUnreservedItem(itemDefn);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.AssignedBuilding.DefnId);

        spot.ReserveBy(Worker);
        ReservedCraftingResourceStorageSpots.Add(spot);
    }

    protected void unreserveBuildingCraftingResourceSpot(StorageSpotData spot)
    {
        spot.Unreserve();
        ReservedCraftingResourceStorageSpots.Remove(spot);
    }


    protected StorageSpotData getNextReservedCraftingResourceStorageSpot()
    {
        Debug.Assert(ReservedCraftingResourceStorageSpots.Count > 0, "getting crafting resource spot, but none remain");
        return ReservedCraftingResourceStorageSpots[0];
    }

    public virtual void Cleanup()
    {
        foreach (var spot in ReservedStorageSpots)
            spot.Unreserve();
        foreach (var spot in ReservedGatheringSpots)
            spot.Unreserve();
        foreach (var spot in ReservedCraftingSpots)
            spot.Unreserve();
        foreach (var spot in ReservedCraftingResourceStorageSpots)
            spot.Unreserve();
    }

    protected bool moveTowards(Vector2 target, float distanceMovedPerSecond, float closeEnough = .1f)
    {
        LastMoveToTarget = target;

        // Move towards target
        Worker.WorldLoc = Vector2.MoveTowards(Worker.WorldLoc, target, distanceMovedPerSecond * GameTime.deltaTime);

        float distance = Vector2.Distance(Worker.WorldLoc, target);
        if (distance <= closeEnough)
        {
            Worker.WorldLoc = target;
            return true; // reached dest
        }

        return false; // not reached
    }

    protected bool moveTowards(BuildingData target, float distanceMovedPerSecond)
    {
        return moveTowards(target.WorldLoc, distanceMovedPerSecond);
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public virtual void OnBuildingDestroyed(BuildingData building)
    {
    }

    // Called when any building is moved; if this Task involves that building then determine
    // what we should do (if anything).
    public virtual void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
    }

    protected bool betterStorageSpotExists(StorageSpotData spot)
    {
        var firstEmptySpot = spot.Building.GetEmptyStorageSpot();
        return firstEmptySpot == null ||
               spot.Building.GetStorageSpotIndex(firstEmptySpot) < spot.Building.GetStorageSpotIndex(spot);
    }

    protected StorageSpotData getBetterStorageSpot(StorageSpotData spot)
    {
        unreserveStorageSpot(spot);
        return reserveStorageSpot(spot.Building);
    }
}
