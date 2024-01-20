using System;
using System.Collections.Generic;
using UnityEngine;

public enum ConstructionState { NotStarted, UnderConstruction, FullyConstructed };

// Not serialized - just used internally by BuildingData
class PrioritizedTask
{
    public WorkerTask Task;
    public float Priority;

    public PrioritizedTask(WorkerTask task, float priority)
    {
        Task = task; Priority = priority;
    }
}

public delegate void OnBuildingTileLocChangedEvent();

[Serializable]
public class BuildingData : BaseData
{
    private BuildingDefn _defn;
    public BuildingDefn Defn
    {
        get
        {
            if (_defn == null)
                _defn = GameDefns.Instance.BuildingDefns[DefnId];
            return _defn;
        }
    }
    public string DefnId;

    // The Town which this Building is in
    public TownData Town;

    public bool IsDestroyed;

    // Which Tile the Building is in
    public int TileX;
    public int TileY;

    static float TileSize = 10;

    // Where the Building is located (== TileLoc * TileSize)
    public Vector3 WorldLoc;

    [NonSerialized] public OnBuildingTileLocChangedEvent OnBuildingTileLocChanged;

    // public ConstructionState ConstructionState;
    // public float PercentBuilt;
    // public bool IsConstructed => !(Defn.CanBeConstructed) || (ConstructionState == ConstructionState.FullyConstructed);

    public List<NeedData> Needs;

    public List<GatheringSpotData> GatheringSpots;
    public List<CraftingSpotData> CraftingSpots;

    // Storage related fields
    public List<StorageAreaData> StorageAreas;
    // public List<WorkerData> WorkersThatReservedStorageSpots;
    public int NumAvailableStorageSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var area in StorageAreas) count += area.NumAvailableSpots;
            return count;
        }
    }

    public bool HasAvailableStorageSpot
    {
        get
        {
            // TODO (PERF): Cache
            foreach (var area in StorageAreas) if (area.HasAvailableSpot) return true;
            return false;
        }
    }

    public bool IsStorageFull => NumAvailableStorageSpots == 0;

    // Resource gathering fields
    public int NumReservedGatheringSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in GatheringSpots) if (spot.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableGatheringSpots => Defn.GatheringSpots.Count - NumReservedGatheringSpots;
    public bool HasAvailableGatheringSpot => NumAvailableGatheringSpots > 0;

    // Crafting fields
    public int NumReservedCraftingSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in CraftingSpots) if (spot.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableCraftingSpots => Defn.CraftingSpots.Count - NumReservedCraftingSpots;
    public bool HasAvailableCraftingSpot => NumAvailableCraftingSpots > 0;

    // For easy tracking
    // public List<NeedData> ConstructionNeeds;

    // If this building can craft items, then CraftingResourceNeeds contains the priority of
    // how much we need different resources to craft those items.  priority is depedent on how
    // many are in storage.
    // * TODO: If another room has broadcast a need for Crafted good X and we can craft it, then
    //   increase priority of resources for crafting it.  note that a settlers-like model may ONLY do these.
    public List<NeedData> CraftingResourceNeeds;

    // How badly we need a courier to clear out our storage
    public NeedData ClearOutStorageNeed;

    // If this building can gather resources, then GatheringNeeds contains the priority of
    // gathering each resource.  e.g.:
    // * if we have many of X and few of Y, then Y may have a higher priority
    // * if our storage is nearly full then all resource gathering is at a reduced priority
    // * TODO: If another room has broadcast a need for resource R and we can gather it, then
    //   increase priority to gather it.  note that a settlers-like model may ONLY do these.
    public List<NeedData> GatheringNeeds;

    public BuildingData(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        DefnId = buildingDefn.Id;
        TileX = tileX;
        TileY = tileY;
        WorldLoc = new Vector3(TileX * TileSize, TileY * TileSize, 0);
    }

    internal void Initialize(TownData town)
    {
        this.Town = town;

        GatheringSpots = new List<GatheringSpotData>();
        for (int i = 0; i < Defn.GatheringSpots.Count; i++)
            GatheringSpots.Add(new GatheringSpotData(this, i));

        CraftingSpots = new List<CraftingSpotData>();
        for (int i = 0; i < Defn.CraftingSpots.Count; i++)
            CraftingSpots.Add(new CraftingSpotData(this, i));

        StorageAreas = new List<StorageAreaData>();
        for (int i = 0; i < Defn.NumStorageAreas; i++)
            StorageAreas.Add(new StorageAreaData(this, i));

        Needs = new List<NeedData>();
        // ConstructionNeeds = new List<NeedData>();
        // CraftingNeeds = new List<NeedData>();
        GatheringNeeds = new List<NeedData>();
        CraftingResourceNeeds = new List<NeedData>();

        // Add need for construction and materials if the building needs to be constructed
        // if (!IsConstructed)
        // {
        //     ConstructionNeeds.Add(new NeedData(this, NeedType.ConstructionWorker, null, Defn.NumConstructorSpots));
        //     foreach (var resource in Defn.ResourcesNeededForConstruction)
        //         ConstructionNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource));
        //     Needs.AddRange(ConstructionNeeds);
        // }

        // Resources should generally only be gathered when there's a need for them e.g. crafting; however we also
        // want a persistent low-priority need for resource gathering so that it's done if nothing else is pending
        if (Defn.CanGatherResources)
        {
            foreach (var resource in Defn.GatherableResources)
                GatheringNeeds.Add(new NeedData(this, NeedType.GatherResource, resource));
            Needs.AddRange(GatheringNeeds);
        }
        if (Defn.CanCraft)
        {
            // Create needs for all craftables; TBD if priorities are set assuming all are crafted, or if only some are prioritized
            // based on AI (ala Settlers) or human selection.
            foreach (var item in Defn.CraftableItems)
                foreach (var resource in item.ResourcesNeededForCrafting)
                    CraftingResourceNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource.Item, resource.Count));
            Needs.AddRange(CraftingResourceNeeds);
        }

        if (Defn.CanStoreItems)
        {
            ClearOutStorageNeed = new NeedData(this, NeedType.ClearStorage);
            ClearOutStorageNeed.NeedCoreType = NeedCoreType.Building;
            Needs.Add(ClearOutStorageNeed);
        }

        if (Defn.CanSellGoods)
        {
            foreach (var item in Defn.GoodsThatCanBeSold)
                Needs.Add(new NeedData(this, NeedType.SellGood, item));
        }
    }

    internal void GetAvailableTasksForWorker(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        addGatheringResourceTasks(availableTasks, worker);
        addCraftingTasks(availableTasks, worker);
        addCourierTasks(availableTasks, worker, allTownNeeds);
        addStorageCleanupTasks(availableTasks, worker, allTownNeeds);
        addAbandonedItemTasks(availableTasks, worker, allTownNeeds);
        addSellGoodTasks(availableTasks, worker, allTownNeeds);
    }

    private void addSellGoodTasks(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.CanSellGoods) return;

        foreach (var need in allTownNeeds)
        {
            if (need.Type != NeedType.SellGood) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // Do we have the item in storage to sell?
            StorageSpotData spotWithItemToSell = GetStorageSpotWithUnreservedItemOfType(need.NeededItem);
            if (spotWithItemToSell == null) continue;

            // Found a storage spot to hold the item
            availableTasks.Add(new PrioritizedTask(WorkerTask_SellGood.Create(worker, need, spotWithItemToSell), need.Priority));
        }
    }

    private void addAbandonedItemTasks(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        // If our workers can ferry items, and there are any items left on the ground, then consider picking them up
        if (!Defn.WorkersCanFerryItems) return;

        foreach (var need in allTownNeeds)
        {
            // Only looking for pickup-abandoned-item needs
            if (need.Type != NeedType.PickupAbandonedItem) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // Minion must have a path to the item
            if (!worker.HasPathToItemOnGround(need.AbandonedItemToPickup)) continue;

            StorageSpotData destinationStorageSpot = Town.GetClosestStorageSpotThatCanStoreItem(need.AbandonedItemToPickup.WorldLocOnGround, need.AbandonedItemToPickup);
            if (destinationStorageSpot == null) continue;

            // Found a storage spot to hold the item
            availableTasks.Add(new PrioritizedTask(WorkerTask_PickupAbandonedItem.Create(worker, need, destinationStorageSpot), need.Priority));
        }
    }

    private void addStorageCleanupTasks(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.WorkersCanFerryItems) return;

        foreach (var need in allTownNeeds)
        {
            // Only looking for cleanup needs
            if (need.Type != NeedType.ClearStorage) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // don't meet the needs of destroyed rooms
            if (need.BuildingWithNeed.IsDestroyed) continue;

            // Minion must have a path to the room
            if (!worker.HasPathToRoom(need.BuildingWithNeed)) continue;

            // need.BuildingWithNeed is saying "I have a need to have items removed from my storage"
            // The worker needs to find the closest item in that building that can/should be removed, and the closest PrimaryStorage spot that is empty and avaialble
            // If those are found, then create a ferryitem task to move it to storage
            StorageSpotData spotWithItemToMove = need.BuildingWithNeed.GetItemToRemoveFromStorage();
            if (spotWithItemToMove == null) continue;

            // TODO: Currently looking at worker's start loc; I think it should look for closest storage spot near where the item is
            StorageSpotData destinationStorageSpot = Town.GetClosestStorageSpotThatCanStoreItem(worker.WorldLoc, spotWithItemToMove.ItemInStorage);
            if (destinationStorageSpot == null) continue;

            // Found a resource that can meet the need - calculate how well this minion can meet the need (score)
            availableTasks.Add(new PrioritizedTask(WorkerTask_FerryItem.Create(worker, spotWithItemToMove, destinationStorageSpot), need.Priority));
        }
    }

    private StorageSpotData GetItemToRemoveFromStorage()
    {
        var itemsUsedForCrafting = new List<StorageSpotData>();

        // Called when a cleaner is looking for something to remove from our storage.
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsEmpty && !spot.IsReserved)
                {
                    // spot contains an unreserved item; do WE need it?
                    var itemIsNeededForCrafting = false;
                    foreach (var need in CraftingResourceNeeds)
                        if (need.NeededItem.Id == spot.ItemInStorage.DefnId)
                        {
                            itemIsNeededForCrafting = true;
                            itemsUsedForCrafting.Add(spot);
                            break;
                        }
                    if (!itemIsNeededForCrafting)
                        return spot;
                }

        return null;
    }

    private void addCourierTasks(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.WorkersCanFerryItems) return;

        // for now, find the first instance where a building needs a resource and the resource is in another building.
        foreach (var need in allTownNeeds)
        {
            // Only looking for crafting, construction, and selling needs
            if (need.Type != NeedType.CraftingOrConstructionMaterial && need.Type != NeedType.SellGood) continue;

            // stockers only meet item needs
            if (need.NeedCoreType != NeedCoreType.Item) continue;

            // If some other minion is already handling fulfilling this need; ignore it
            // if (need.State != NeedState.unmet) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // don't meet the needs of destroyed rooms
            if (need.BuildingWithNeed.IsDestroyed) continue;

            // Minion must have a path to the room
            if (!worker.HasPathToRoom(need.BuildingWithNeed)) continue;

            // Find closest accessible resource to this minion that can meet the need.  This function ensures that
            // there is a path from the room to the resource.
            ClosestStorageSpotWithItem closestStorageSpotWithItem = Town.getClosestItemOfType(need.NeededItem, need.ItemClass, need.BuildingWithNeed);
            if (closestStorageSpotWithItem.Distance == float.MaxValue)
            {
                // No resource available to meet the need.  Ignore it, hope we can at least meet antoher need
                continue;
            }

            // Can't deliver items to a room that's full
            var destinationStorageSpot = need.BuildingWithNeed.GetStorageCellThatCanStoreItem(closestStorageSpotWithItem.StorageSpot.ItemInStorage.Defn);
            if (destinationStorageSpot == null)
                continue;

            // Found a resource that can meet the need - calculate how well this minion can meet the need (score)
            float needScore = need.PriorityOfMeetingItemNeed(worker, destinationStorageSpot.Building, closestStorageSpotWithItem.Distance);
            if (needScore > 0)
                availableTasks.Add(new PrioritizedTask(WorkerTask_FerryItem.Create(worker, closestStorageSpotWithItem.StorageSpot, destinationStorageSpot), needScore));
        }
    }

    private StorageSpotData GetStorageCellThatCanStoreItem(ItemDefn defn)
    {
        // PORT: Before, N items could be stored in a storage spot; now only one can.  May need to revisit that.
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                // if (spot.CanStoreItem(defn))
                if (spot.IsEmptyAndAvailable)
                    return spot;
        return null;
    }

    private void addGatheringResourceTasks(List<PrioritizedTask> availableTasks, WorkerData worker)
    {
        if (!Defn.CanGatherResources) return;
        if (IsStorageFull) return;

        foreach (var need in GatheringNeeds)
        {
            if (need.Priority == 0) continue; // no active need for the resource (e.g. already being fully met)

            BuildingData targetMine = Town.getNearestResourceSource(worker, need.NeededItem);
            if (targetMine == null) continue; // no building to gather from

            // Good to go - add the task as a possible choice
            availableTasks.Add(new PrioritizedTask(WorkerTask_GatherResource.Create(worker, need.NeededItem, targetMine), need.Priority));
        }
    }

    private void addCraftingTasks(List<PrioritizedTask> availableTasks, WorkerData worker)
    {
        if (!Defn.CanCraft) return;
        if (IsStorageFull) return;
        if (NumAvailableCraftingSpots == 0) return;

        foreach (var itemToCraft in Defn.CraftableItems)
        {
            var priority = getPriorityOfCraftingItem(itemToCraft);
            if (priority == 0) continue;

            // Do we have the items in storage to craft the item?
            // TODO: Option to spin off a "go get item from storage" task if couriers aren't doing it?
            if (!hasUnreservedResourcesInStorageToCraftItem(itemToCraft)) continue;

            // Do we have storage for the crafted item?
            // TODO: Option to move item from our storage to a storage room if couriers aren't doing it?
            bool isImplicitGood = itemToCraft.GoodType == GoodType.implicitGood;
            if (!isImplicitGood && !HasAvailableStorageSpot) continue;

            // Good to go - add the task as a possible choice
            availableTasks.Add(new PrioritizedTask(WorkerTask_CraftItem.Create(worker, itemToCraft), priority));
        }
    }

    public int GetStorageSpotIndex(StorageSpotData spotToCheck)
    {
        Debug.Assert(spotToCheck.Building == this, "wrong building");
        var index = 0;
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (spot == spotToCheck)
                    return index;
                else
                    index++;
        Debug.Assert(false, "Failed to find storage spot");
        return -1;
    }


    public ItemData GetUnreservedItemOfType(ItemDefn itemDefn, ItemClass itemClass = ItemClass.Unset)
    {
        var spot = GetStorageSpotWithUnreservedItemOfType(itemDefn, itemClass);
        return spot == null ? null : spot.ItemInStorage;
    }

    public StorageSpotData GetStorageSpotWithUnreservedItemOfType(ItemDefn itemDefn, ItemClass itemClass = ItemClass.Unset)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInStorage != null && spot.ItemInStorage.DefnId == itemDefn.Id)// && spot.ItemInStorage.Defn.ItemClass == itemClass)
                    return spot;

        return null;
    }

    float getPriorityOfCraftingItem(ItemDefn itemToCraft)
    {
        // If user exlicitly chose to craft an item, 
        //      and it's this item then value == 1; else value = .1
        // else if user specified 0-1 priority for item then use that (e.g. they used a slider)
        // else if Town has a specific need for the item (from another building) then use its priority
        //      ** If I can cascade this, then I've recreated Settlers/WinEcon **
        // else look at items in storage
        //      if <5% of storage contains item then priority = 1
        //      else if <25% of storage contains item then priority = .5
        //      else if <50% of storage contains item then priority = .25
        //      else priority = .1
        return 0.5f;
    }

    private bool hasUnreservedResourcesInStorageToCraftItem(ItemDefn item)
    {
        foreach (var resource in item.ResourcesNeededForCrafting)
            if (!hasUnreservedItemsInStorage(resource.Item, resource.Count))
                return false;
        return true;
    }

    private bool hasUnreservedItemsInStorage(ItemDefn itemDefn, int count)
    {
        int num = 0;
        foreach (var area in StorageAreas)
            num += area.NumUnreservedItemsInStorage(itemDefn);
        return num >= count;
    }

    /**
        returns true if this building supports gathering the required resource AND there's
        an available gathering spot
    */
    internal bool ResourceCanBeGatheredFromHere(ItemDefn itemDefn)
    {
        return Defn.ResourcesCanBeGatheredFromHere &&
                Defn.ResourcesThatCanBeGatheredFromHere.Contains(itemDefn) &&
                HasAvailableGatheringSpot;
    }

    public void Update()
    {
        UpdateNeedPriorities();
    }

    public void UpdateNeedPriorities()
    {
        // Hardcoded 9 storage spots per storage area
        float percentFull = 1 - (float)(NumAvailableStorageSpots) / (Defn.NumStorageAreas * 9);

        if (Defn.CanStoreItems)
        {
            if (Defn.IsPrimaryStorage)
                ClearOutStorageNeed.Priority = 0;
            else
            {
                // unless close to full, Cleanup tasks are lower priority than filling crafting need tasks
                if (percentFull < .75)
                    percentFull /= 5f;

                ClearOutStorageNeed.Priority = percentFull;

                // if we're a crafting room then we have a higher priority than non-crafting rooms (e.g. woodcutter) to clear storage
                // so that we can craft more
                if (Defn.CanCraft)
                    ClearOutStorageNeed.Priority *= 1.5f;
            }
        }

        foreach (var need in Needs)
            if (need.Type == NeedType.SellGood)
            {
                // Can be set by user; e.g. they can increase priority of selling wood if they want to
                need.Priority = 0.5f;
            }

        foreach (var need in GatheringNeeds)
        {
            if (need.IsBeingFullyMet)
            {
                need.Priority = 0;
                continue;
            }
            need.Priority = 1;
            // if we have a lot of them then reduce priority
            int numOfNeededItemAlreadyInStorage = NumItemsInStorage(need.NeededItem);
            if (percentFull > .99f)
                need.Priority = 0; // full
            else if (percentFull > .75f)
                need.Priority *= .5f; // storage is mostly full of any items
            else if (percentFull > .5f)
                need.Priority *= .75f; // storage is somewhat full of any items
            else if (percentFull > .25f)
                need.Priority *= .875f; // storage has some of item already
            else if (percentFull > .1f)
                need.Priority *= .9f; // storage has a couple of item already
            else if (numOfNeededItemAlreadyInStorage > Defn.NumStorageAreas / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > Defn.NumStorageAreas / 4)
                need.Priority *= .75f; // storage is 25%-50% full of the needed item
        }

        foreach (var need in CraftingResourceNeeds)
        {
            // if (need.IsBeingFullyMet)
            // {
            //     need.Priority = 0;
            //     continue;
            // }
            need.Priority = 1;
            // if we have a lot of them then reduce priority
            int numOfNeededItemAlreadyInStorage = NumItemsInStorage(need.NeededItem);
            if (percentFull > .99f)
                need.Priority = 0; // full
            else if (percentFull > .75f)
                need.Priority *= .5f; // storage is mostly full of any items
            else if (percentFull > .5f)
                need.Priority *= .75f; // storage is somewhat full of any items
            else if (percentFull > .25f)
                need.Priority *= .875f; // storage has some of item already
            else if (percentFull > .1f)
                need.Priority *= .9f; // storage has a couple of item already
            else if (numOfNeededItemAlreadyInStorage > Defn.NumStorageAreas / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > Defn.NumStorageAreas / 4)
                need.Priority *= .75f; // storage is 25%-50% full of the needed item
        }
    }

    public int NumItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var area in StorageAreas)
            count += area.NumItemsInStorage(itemDefn);
        return count;
    }

    public void AddItemToStorage(ItemData item)
    {
        Debug.Assert(!IsStorageFull, "Adding item to full storage " + DefnId);

        // Find an empty storagespot
        StorageSpotData emptySpot = GetEmptyStorageSpot();
        emptySpot.AddItem(item);

        Debug.Assert(!emptySpot.IsReserved, "AddItemToStorage: Got a reserved spot");
    }

    public StorageSpotData GetEmptyStorageSpot()
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (spot.IsEmptyAndAvailable)
                    return spot;
        return null;
    }

    // internal void RemoveItemFromStorage(ItemData item)
    // {
    //     foreach (var spot in StorageSpots)
    //         if (spot.ItemInStorage == item)
    //         {
    //             removeItemFromSpot(spot);
    //             return;
    //         }
    //     Debug.Assert(false, "removed item that isn't in storage");
    // }

    internal void Debug_RemoveAllItemsFromStorage()
    {
        foreach (var area in StorageAreas)
            area.Debug_RemoveAllItemsFromStorage();
    }

    // private void removeItemFromSpot(StorageSpotData spot)
    // {
    //     Debug.Assert(!spot.IsReserved, "Debug_RemoveAllItemsFromStorage: Got a reserved spot");
    //     var item = spot.ItemInStorage;
    //     spot.ItemInStorage = null;
    //     // spot.OnItemRemoved?.Invoke(item);
    // }

    // public void SelectItemToCraft(ItemDefn item)
    // {
    //     Debug.Assert(Defn.CanCraft, "must be able to craft");

    //     // Clear out current crafting need in case item has changed
    //     CraftingNeeds.ForEach(need => need.Cancel());
    //     CraftingNeeds.Clear();

    //     // Add needs for each of the resources needed to craft the item
    //     foreach (var resource in item.ResourcesNeededForCrafting)
    //         CraftingNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource));
    //     Needs.AddRange(CraftingNeeds);
    // }

    // public void OnConstructionComplete()
    // {
    //     ConstructionState = ConstructionState.FullyConstructed;
    //     PercentBuilt = 1;
    //     ConstructionNeeds.ForEach(need => need.Cancel());
    //     ConstructionNeeds.Clear();
    // }

    internal GatheringSpotData ReserveGatheringSpot(WorkerData worker)
    {
        // Find first unreserved gathering spot
        foreach (var spot in GatheringSpots)
            if (!spot.IsReserved)
            {
                spot.ReserveBy(worker);
                return spot;
            }
        Debug.Assert(false, "Reserving spot but none available");
        return null;
    }

    internal void UnreserveGatheringSpot(WorkerData worker)
    {
        foreach (var spot in GatheringSpots)
            if (spot.ReservedBy == worker)
            {
                Debug.Assert(spot.IsReserved, "Gathering spot has reservedby but isreserved=false");
                spot.Unreserve();
                return;
            }

        Debug.Assert(false, "Unreserving gathering spot which isn't reserved by Worker");
    }

    internal CraftingSpotData ReserveCraftingSpot(WorkerData worker)
    {
        // Debug.Assert(WorkersThatReservedCraftingSpots.Count < Defn.CraftingSpots.Count, "Assigning too many workers to crafting spots");
        // WorkersThatReservedCraftingSpots.Add(worker);


        // Find first unreserved crafting spot
        foreach (var spot in CraftingSpots)
            if (!spot.IsReserved)
            {
                spot.ReserveBy(worker);
                return spot;
            }
        Debug.Assert(false, "Reserving craftingspot but none available");
        return null;
    }

    internal void UnreserveCraftingSpot(WorkerData worker)
    {
        // Debug.Assert(WorkersThatReservedCraftingSpots.Contains(worker), "Unreserving crafting spot but worker doesn't have one reserved");
        // WorkersThatReservedCraftingSpots.Remove(worker);

        foreach (var spot in CraftingSpots)
            if (spot.ReservedBy == worker)
            {
                Debug.Assert(spot.IsReserved, "Crafting spot has reservedby but isreserved=false");
                spot.Unreserve();
                return;
            }

        Debug.Assert(false, "Unreserving Crafting spot which isn't reserved by Worker");
    }

    internal StorageSpotData ReserveStorageSpot(WorkerData worker)
    {
        Debug.Assert(HasAvailableStorageSpot, "Assigning too many workers to storage spots");
        var spot = GetEmptyStorageSpot();
        if (spot != null)
            spot.ReserveBy(worker);
        return spot;
    }

    // internal void UnreserveStorageSpot(WorkerData worker)
    // {
    //     // Note: Workers can only reserve one spot each; fine for now
    //     foreach (var area in StorageAreas)
    //         foreach (var spot in area.StorageSpots)
    //             if (spot.ReservedBy == worker)
    //             {
    //                 spot.Unreserve();
    //                 spot.RemoveItem();
    //                 // WorkersThatReservedStorageSpots.Remove(worker);
    //                 return;
    //             }
    //     Debug.Assert(false, "Unreserving storage spot but worker doesn't have one reserved");
    // }

    internal ItemData GetUnreservedItemInStorage(ItemDefn item)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInStorage != null && spot.ItemInStorage.DefnId == item.Id)
                    return spot.ItemInStorage;
        return null;
    }

    internal StorageSpotData GetStorageSpotWithUnreservedItem(ItemDefn item)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInStorage != null && spot.ItemInStorage.DefnId == item.Id)
                    return spot;
        return null;
    }

    internal void Destroy()
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");
        Debug.Assert(this != Town.Camp, "Can't destroy camp");

        IsDestroyed = true;

        // Cancel building's needs
        foreach (var need in Needs)
            need.Cancel();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.Workers)
            worker.OnBuildingDestroyed(this);

        // Move items that were in storage onto the ground here
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsEmpty)
                {
                    Town.AddItemToGround(spot.ItemInStorage, spot.WorldLoc);// + WorldLoc);
                    spot.RemoveItem();
                }
    }

    internal void MoveTo(int tileX, int tileY)
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");

        TileX = tileX;
        TileY = tileY;

        Vector2 previousWorldLoc = WorldLoc;
        WorldLoc = new Vector2(TileX * TileSize, TileY * TileSize);
        UpdateWorldLoc();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.Workers)
            worker.OnBuildingMoved(this, previousWorldLoc);

        OnBuildingTileLocChanged?.Invoke();
    }

    internal void UpdateWorldLoc()
    {
        // TODO: UGH
        foreach (var area in StorageAreas)
            area.UpdateWorldLoc();
        foreach (var spot in CraftingSpots)
            spot.UpdateWorldLoc();
        foreach (var spot in GatheringSpots)
            spot.UpdateWorldLoc();
    }

    public List<DistanceToBuilding> RoomsByDistance = new List<DistanceToBuilding>();

    public void UpdateDistanceToRooms()
    {
        RoomsByDistance.Clear();
        foreach (var building in Town.Buildings)
        {
            //  if (room.Defn.IsScriptedEventRoom || room.BuildingData.IsInStash) continue;
            float distance = distanceToRoom(building);
            if (distance < int.MaxValue)
            {
                RoomsByDistance.Add(new DistanceToBuilding() { Building = building, Distance = distance });
            }
        }
        RoomsByDistance.Sort((a, b) => (int)(a.Distance - b.Distance));
    }

    private float distanceToRoom(BuildingData destRoom)
    {
        return Vector3.Distance(WorldLoc, destRoom.WorldLoc);
    }

    public DistanceToBuilding getRoomDistance(BuildingData building)
    {
        foreach (var roomDist in RoomsByDistance)
            if (roomDist.Building == building)
                return roomDist;
        return null;
    }

    public float getDistanceToBuilding(BuildingData room)
    {
        DistanceToBuilding roomDist = getRoomDistance(room);
        return roomDist != null ? roomDist.Distance : float.MaxValue;
    }
}