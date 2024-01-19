using System;
using System.Collections.Generic;
using UnityEngine;

public delegate void OnItemAddedToGroundEvent(ItemData item);

[Serializable]
public class TownData : BaseData
{
    private TownDefn _defn;
    public TownDefn Defn
    {
        get
        {
            if (_defn == null)
                _defn = GameDefns.Instance.TownDefns[DefnId];
            return _defn;
        }
    }
    public string DefnId;

    // Position in the WorldMap
    public int WorldX => Defn.WorldX;
    public int WorldY => Defn.WorldY;

    public int Gold;
    [NonSerialized] public OnItemAddedToGroundEvent OnItemAddedToGround;
    public TownState State;
    public bool CanEnter => State == TownState.Available || State == TownState.InProgress;

    // Current Map
    public List<TileData> Tiles = new List<TileData>();
    public List<WorkerData> Workers = new List<WorkerData>();

    public List<BuildingData> Buildings = new List<BuildingData>();

    public BuildingData Camp;
    public List<ItemData> ItemsOnGround = new List<ItemData>();

    public TownData(TownDefn townDefn, TownState startingState)
    {
        DefnId = townDefn.Id;
        State = startingState;
    }

    public void InitializeOnFirstEnter()
    {
        Tiles.Clear();
        string[] tiles = Defn.Tiles.Split(",");
        Debug.Assert(tiles.Length == Defn.Width * Defn.Height, "wrong num tiles");
        for (int y = 0; y < Defn.Height; y++)
            for (int x = 0; x < Defn.Width; x++)
                Tiles.Add(new TileData(x, y, tiles[y * Defn.Width + x]));

        Buildings.Clear();
        Workers.Clear();
        foreach (var tbDefn in Defn.Buildings)
        {
            if (!tbDefn.IsEnabled) continue;
            var building = new BuildingData(tbDefn.Building, tbDefn.TileX, tbDefn.TileY);
            building.Initialize(this);
            Buildings.Add(building);

            Tiles[tbDefn.TileY * Defn.Width + tbDefn.TileX].BuildingInTile = building;

            if (building.Defn.BuildingClass == BuildingClass.Camp)
                Camp = building;

            for (int i = 0; i < tbDefn.NumWorkersStartAtBuilding; i++)
                Workers.Add(new WorkerData(building));
            foreach (var item in tbDefn.StartingItemsInBuilding)
                for (int i = 0; i < item.Count; i++)
                    building.AddItemToStorage(new ItemData() { DefnId = item.Item.Id });
        }
        UpdateDistanceToRooms();
    }

    internal void TestMoveBuilding(int test)
    {
        MoveBuilding(Buildings[test], 2, 1);
    }

    public void DestroyBuilding(BuildingData building)
    {
        Tiles[building.TileY * Defn.Width + building.TileX].BuildingInTile = null;

        Buildings.Remove(building);
        building.Destroy();
        UpdateDistanceToRooms();
    }

    public void DestroyWorker(WorkerData worker)
    {
        Workers.Remove(worker);
        worker.Destroy();
    }

    public void MoveBuilding(BuildingData building, int tileX, int tileY)
    {
        Tiles[building.TileY * Defn.Width + building.TileX].BuildingInTile = null;
        building.MoveTo(tileX, tileY);
        Tiles[tileY * Defn.Width + tileX].BuildingInTile = building;
        UpdateDistanceToRooms();
    }

    public void Update()
    {
        // TODO (PERF): Update on change
        foreach (var building in Buildings)
            building.UpdateWorldLoc();

        foreach (var building in Buildings)
            building.Update();

        FindTasksForNextIdleWorker();

        foreach (var worker in Workers)
            worker.Update();
    }

    /**
        TODO (FUTURE): I've made this to only find the optimal task for the first idle worker, rather than
        for all idle workers.  Reason: the act of assigning an idle worker itself changes priorities, and the 
        code below doesn't account for that.  afaict, it looks fine to do one idle worker per frame.  If this 
        becomes a problem in the future then I'll need to revisit this.  Note that I've left in support for >
        idle worker handling below to ease that future need - it means a lot of extra work may be happening 
        below, but again: it seems fine for now. also of note, it may be better to spread out processing like this anyways
     */
    private void FindTasksForNextIdleWorker()
    {
        // todo (perf): keep list of idle
        var idleWorkers = new List<WorkerData>();
        foreach (var worker in Workers) if (worker.IsIdle) idleWorkers.Add(worker);
        if (idleWorkers.Count == 0)
            return;

        // TODO (PERF): Keep list of all needs
        var allNeeds = new List<NeedData>();
        foreach (var building in Buildings)
            allNeeds.AddRange(building.Needs);

        List<PrioritizedTask> availableTasks = new List<PrioritizedTask>();
        foreach (var worker in idleWorkers)
            worker.AssignedBuilding?.GetAvailableTasksForWorker(availableTasks, worker, allNeeds);

        // TODO: Add worker self tasks; hunger thirst, return to assignedbuilding, etc

        if (availableTasks.Count == 0)
            return;

        availableTasks.Sort((a, b) => (int)((b.Priority - a.Priority) * 1000));

        // availableTasks now contains every IdleWorker+BuildingNeed combination, sorted by priority
        // Go through the list in priority order, assigning to workers as we go
        foreach (var prioritizedTask in availableTasks)
        {
            var worker = prioritizedTask.Task.Worker;
            // if (!worker.IsIdle)
            //     continue;   // If not idle, then we must have assigned them a task earlier in the list

            // If another worker was just assigned to perform the same task, then it may not longer be valid
            // (e.g. its need is now being fully met)
            // if (!prioritizedTask.Task.IsStillValid())
            //     continue;

            // Assign the task and start it
            worker.CurrentTask = prioritizedTask.Task;
            worker.CurrentTask.Start();
            // worker.AssignedBuilding.UpdateNeedPriorities();

            idleWorkers.Remove(worker);
            if (idleWorkers.Count == 0)
                break; // no more idle workers

            // For now, just do one worker per frame.  When I support > 1 per frame, I need to 
            // check for isstillvalid above.
            return;
        }
    }

    internal BuildingData getNearestResourceSource(WorkerData worker, ItemDefn itemDefn)
    {
        BuildingData closestBuildingWithResourceAndGatheringSpot = null;
        float closestBuildingDistance = float.MaxValue;

        foreach (var building in Buildings)
        {
            if (building.ResourceCanBeGatheredFromHere(itemDefn))
            {
                float distanceToBuilding = worker.DistanceToBuilding(building);
                if (distanceToBuilding < closestBuildingDistance)
                {
                    closestBuildingWithResourceAndGatheringSpot = building;
                    closestBuildingDistance = distanceToBuilding;
                }
            }
        }
        return closestBuildingWithResourceAndGatheringSpot;
    }

    internal void AddItemToGround(ItemData item, Vector2 pos)
    {
        ItemsOnGround.Add(item);
        item.WorldLocOnGround = pos;
        OnItemAddedToGround?.Invoke(item);
    }

    // Only returns Primary Storage rooms (Camp, STorageRoom)
    internal StorageSpotData GetClosestStorageSpotThatCanStoreItem(Vector3 worldLoc, ItemData itemInStorage)
    {
        // TODO: more performant distance checking
        StorageSpotData closestSpot = null;
        float closestSpotDistance = float.MaxValue;

        foreach (var building in Buildings)
            if (building.Defn.CanStoreItems && building.Defn.IsPrimaryStorage && building.HasAvailableStorageSpot)
            {
                var spot = building.GetEmptyStorageSpot();
                var dist = Vector2.Distance(worldLoc, spot.WorldLoc);
                if (closestSpot == null || dist < closestSpotDistance)
                {
                    closestSpot = spot;
                    closestSpotDistance = dist;
                }
            }
        return closestSpot;
    }

    /**
     * Return the closest Cell that holds an un-reserved resource of the specified type.  This is used to find
     * A resource that a stocker can bring to a room that needs the resource.
     */
    public ClosestStorageSpotWithItem getClosestItemOfType(ItemDefn itemDefn, ItemClass itemClass, BuildingData startingRoom)
    {
        foreach (var roomDist in startingRoom.RoomsByDistance)
        {
            BuildingData building = roomDist.Building;
            //  DebugMgr.Assert(room != null, "huh?");
            if (building == startingRoom) continue;
            //    if (!room.IsBuilt) continue;
            if (!building.Defn.CanStoreItems) continue;
            // for now, don't care about cell-based distance; just room-based.  can add that later if it looks odd

            // If 'room' itself has a need for resourceType then don't consider it for pickup (e.g. don't move food from room that has hungry assigned entity)
            bool ignoreRoom = false;
            foreach (var need in building.Needs)
            {
                // only check non-item-class-based item needs.  If looking for e.g any item of type food then use getclosestItemOfClass instead...
                if (itemDefn != null && need.Type != NeedType.GatherResource && need.NeedCoreType == NeedCoreType.Item && need.NeededItem != null && need.NeededItem.Id == itemDefn.Id && need.State == NeedState.unmet)
                {
                    ignoreRoom = true;
                    break;
                }
            }

            // TODO: Removed in porting
            // if ((itemClass == ItemClass.Edible || itemClass == ItemClass.Drinkable) && room.Assignable != null && !room.Assignable.HasSurplusOfItemClass(ItemClass.Edible))
            //     ignoreRoom = true;

            if (ignoreRoom)
                continue;

            var storageSpotWithItem = building.GetStorageSpotWithUnreservedItemOfType(itemDefn, itemClass);
            if (storageSpotWithItem != null)
                return new ClosestStorageSpotWithItem(storageSpotWithItem, roomDist);
        }

        // no resource accessible
        ClosestStorageSpotWithItem closestItem = new ClosestStorageSpotWithItem();
        closestItem.Distance = float.MaxValue;
        return closestItem;
    }

    public void UpdateDistanceToRooms()
    {
        // TODO (PERF): Can cut this time in 1/2 since room A->B distance is same as room B->A distance.
        foreach (BuildingData building in Buildings)// PlacedBuildings)
        {
            // if (room.Defn.IsScriptedEventRoom || room.BuildingData.IsInStash)
            // continue;
            building.UpdateDistanceToRooms();
        }
    }

    internal int Chart_GetNumOfItemInTown(string itemId)
    {
        int numInStorage = 0;
        foreach (var building in Buildings)
            numInStorage += building.NumItemsInStorage(GameDefns.Instance.ItemDefns[itemId]);

        // see how many are being carried
        var numBeingCarried = 0;
        foreach (var worker in Workers)
            if (worker.CurrentTask.IsCarryingItem(itemId))
                numBeingCarried++;
        return numInStorage + numBeingCarried;
    }


    internal float Chart_GetNeedForItem(string itemId)
    {
        // TBD: Average, total, or max?  Total for now
        var total = 0f;
        foreach (var building in Buildings)
            foreach (var need in building.Needs)
                if (need.Type == NeedType.CraftingOrConstructionMaterial && need.NeededItem.Id == itemId)
                    total += need.Priority;
        return total;
    }
}