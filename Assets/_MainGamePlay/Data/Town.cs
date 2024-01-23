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
    [NonSerialized] public Action<ItemData> OnItemRemovedFromGround;
    [NonSerialized] public Action<ItemData> OnItemSold;

    [NonSerialized] public Action<BuildingData> OnBuildingAdded;
    [NonSerialized] public Action<WorkerData> OnWorkerCreated;
    [NonSerialized] public List<PrioritizedTask> availableTasks;

    // For debugging purposes
    [NonSerialized] public List<PrioritizedTask> LastSeenPrioritizedTasks = new();

    public TownState State;
    public bool CanEnter => State == TownState.Available || State == TownState.InProgress;

    // Current Map
    public List<TileData> Tiles = new();
    public List<WorkerData> Workers = new();

    public List<BuildingData> Buildings = new();

    public BuildingData Camp;
    public List<ItemData> ItemsOnGround = new();
    public List<NeedData> otherTownNeeds = new();

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
        otherTownNeeds.Clear();
        foreach (var tbDefn in Defn.Buildings)
        {
            if (!tbDefn.IsEnabled) continue;

            var building = ConstructBuilding(tbDefn.Building, tbDefn.TileX, tbDefn.TileY);
            if (building.Defn.BuildingClass == BuildingClass.Camp)
                Camp = building;

            for (int i = 0; i < tbDefn.NumWorkersStartAtBuilding; i++)
                CreateWorkerInBuilding(building);

            foreach (var item in tbDefn.StartingItemsInBuilding)
                for (int i = 0; i < item.Count; i++)
                    building.AddItemToStorageSpot(new ItemData() { DefnId = item.Item.Id }, building.GetEmptyStorageSpot());
        }
        UpdateDistanceToRooms();
    }

    public void CreateWorkerInBuilding(BuildingData building)
    {
        var worker = new WorkerData(building);
        Workers.Add(worker);
        OnWorkerCreated?.Invoke(worker);
    }

    internal void TestMoveBuilding(int test)
    {
        MoveBuilding(Buildings[test], 2, 1);
    }

    public BuildingData ConstructBuilding(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        var building = new BuildingData(buildingDefn, tileX, tileY);
        building.Initialize(this);
        Buildings.Add(building);
        Tiles[tileY * Defn.Width + tileX].BuildingInTile = building;
        UpdateDistanceToRooms();

        OnBuildingAdded?.Invoke(building);

        return building;
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
        GameTime.Update();

        // TODO (PERF): Update on change
        foreach (var building in Buildings)
            building.UpdateWorldLoc();

        foreach (var building in Buildings)
            building.Update();

        // e.g. pick up items on the ground
        updateTownNeeds();

        FindTasksForNextIdleWorker();

        foreach (var worker in Workers)
            worker.Update();
    }

    private void updateTownNeeds()
    {
        foreach (var need in otherTownNeeds)
        {
            if (need.IsBeingFullyMet)
                need.Priority = 0;
            else
                need.Priority = Math.Min(GameTime.time - need.StartTimeInSeconds / 10f, .25f);
        }
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
        List<WorkerData> idleWorkers = new();
        foreach (var worker in Workers) if (worker.IsIdle) idleWorkers.Add(worker);
        if (idleWorkers.Count == 0)
            return;

        // TODO (PERF): Keep list of all needs
        List<NeedData> allNeeds = new();
        foreach (var building in Buildings)
            allNeeds.AddRange(building.Needs);

        // Add needs to pick up any items abandoned on the ground
        allNeeds.AddRange(otherTownNeeds);

        if (availableTasks == null) availableTasks = new();
        availableTasks.Clear();
        foreach (var worker in idleWorkers)
            worker.AssignedBuilding?.GetAvailableTasksForWorker(availableTasks, worker, allNeeds);

        // TODO: Add worker self tasks; hunger thirst, return to assignedbuilding, etc

        if (availableTasks.Count == 0)
            return;

        availableTasks.Sort((a, b) => (int)((b.Priority - a.Priority) * 1000));

        // For debugging
        LastSeenPrioritizedTasks ??= new();
        LastSeenPrioritizedTasks.Clear();
        LastSeenPrioritizedTasks.AddRange(availableTasks);

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

        // Add a need to pick up the item.  This will be removed when the item is picked up
        otherTownNeeds.Add(new NeedData(item));
    }

    public void RemoveItemFromGround(ItemData item)
    {
        Debug.Assert(ItemsOnGround.Contains(item), "removing item from ground that isn't there");
        ItemsOnGround.Remove(item);
        OnItemRemovedFromGround?.Invoke(item);

        // Remove the need to pick up the item
        foreach (var need in otherTownNeeds)
            if (need.Type == NeedType.PickupAbandonedItem && need.AbandonedItemToPickup == item)
            {
                otherTownNeeds.Remove(need);
                break;
            }
    }

    // Only returns Primary Storage rooms (Camp, STorageRoom)
    internal StorageSpotData GetClosestPrimaryStorageSpotThatCanStoreItem(Vector3 worldLoc)
    {
        // TODO: more performant distance checking
        StorageSpotData closestSpot = null;
        float closestSpotDistance = float.MaxValue;

        foreach (var building in Buildings)
            if (building.Defn.CanStoreItems && building.Defn.IsPrimaryStorage && building.HasAvailableStorageSpot)
            {
                var spot = building.GetClosestEmptyStorageSpot(worldLoc, out float dist);
                if (dist < closestSpotDistance)
                {
                    closestSpot = spot;
                    closestSpotDistance = dist;
                }
            }
        return closestSpot;
    }

    internal StorageSpotData GetClosestStorageSpotThatCanStoreItem(Vector3 worldLoc)
    {
        // TODO: more performant distance checking
        StorageSpotData closestSpot = null;
        float closestSpotDistance = float.MaxValue;

        foreach (var building in Buildings)
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot)
            {
                var spot = building.GetClosestEmptyStorageSpot(worldLoc, out float dist);
                if (dist < closestSpotDistance)
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

    internal void UnassignWorkerFromBuilding(BuildingData data)
    {
        WorkerData worker = GetWorkerInBuilding(data);
        worker?.AssignToBuilding(Camp);
    }

    internal void AssignWorkerToBuilding(BuildingData data)
    {
        WorkerData worker = GetWorkerInBuilding(Camp);
        worker?.AssignToBuilding(data);
    }

    private WorkerData GetWorkerInBuilding(BuildingData building)
    {
        foreach (var worker in Workers)
            if (worker.AssignedBuilding == building)
                return worker;
        return null;
    }

    internal int NumBuildingWorkers(BuildingData building)
    {
        // TODO: Store in buidlingdata instead
        int num = 0;
        foreach (var worker in Workers)
            if (worker.AssignedBuilding == building)
                num++;
        return num;
    }

    internal bool WorkerIsAvailable()
    {
        // called when a building is requesting an available worker be assigned to it
        // For now, assignment is done from Camp, so just check if Camp has any workers
        return NumBuildingWorkers(Camp) > 0;
    }

    internal void ItemSold(ItemData item)
    {
        // todo: multipliers e.g. from research
        Gold += item.Defn.BaseSellPrice;
        OnItemSold?.Invoke(item);
    }

    internal bool PlayerCanAffordBuilding(BuildingDefn buildingDefn)
    {
        // verify that the building's construction requirements (wood etc) are in the Town and unreserved
        foreach (var item in buildingDefn.ResourcesNeededForConstruction)
        {
            int numInStorage = Chart_GetNumOfItemInTown(item.Item.Id);
            if (numInStorage < item.Count)
                return false;
        }
        return true;
    }
}