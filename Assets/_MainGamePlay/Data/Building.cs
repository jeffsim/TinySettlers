using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;

public enum ConstructionState { NotStarted, UnderConstruction, FullyConstructed };

public delegate void OnBuildingTileLocChangedEvent();

[Serializable]
public class DistanceToBuilding
{
    public float Distance;
    public BuildingData Building;
}

[Serializable]
public class BuildingData : BaseData
{
    public override string ToString() => Defn.FriendlyName + " (" + InstanceId + ")";

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

    public BuildingTaskMgrData BuildingTaskMgr;

    public bool IsDestroyed;

    // Which Tile the Building is in
    public int TileX;
    public int TileY;

    static float TileSize = 10;

    public bool IsPaused;

    // Where the Building is located (== TileLoc * TileSize)
    public Vector3 WorldLoc;

    [NonSerialized] public OnBuildingTileLocChangedEvent OnBuildingTileLocChanged;

    // public ConstructionState ConstructionState;
    // public float PercentBuilt;
    // public bool IsConstructed => !(Defn.CanBeConstructed) || (ConstructionState == ConstructionState.FullyConstructed);

    public List<DistanceToBuilding> OtherBuildingsByDistance = new();

    public List<NeedData> Needs;

    public List<GatheringSpotData> GatheringSpots;
    public List<CraftingSpotData> CraftingSpots;

    // Storage related fields
    public List<StorageAreaData> StorageAreas;
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
    public int NumStorageSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var area in StorageAreas) count += area.StorageSpots.Count;
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

    // storage
    public int NumReservedStorageSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var area in StorageAreas)
                foreach (var spot in area.StorageSpots)
                    if (spot.IsReserved)
                        count++;
            return count;
        }
    }

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
    // * TODO: If another building has broadcast a need for Crafted good X and we can craft it, then
    //   increase priority of resources for crafting it.  note that a settlers-like model may ONLY do these.
    public List<NeedData> CraftingResourceNeeds;

    // If this buidling can sell items, then it needs those items to be in storage so that they can be sold
    public List<NeedData> SellingGoodNeeds;

    // How badly we need a courier to clear out our storage
    public NeedData ClearOutStorageNeed;

    // If this building can gather resources, then GatheringNeeds contains the priority of
    // gathering each resource.  e.g.:
    // * if we have many of X and few of Y, then Y may have a higher priority
    // * if our storage is nearly full then all resource gathering is at a reduced priority
    // * TODO: If another building has broadcast a need for resource R and we can gather it, then
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
        Town = town;

        BuildingTaskMgr = new(this);

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
        GatheringNeeds = new();
        CraftingResourceNeeds = new();
        SellingGoodNeeds = new();

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
            {
                // Add Need for "I need to craft (if resources are in room)"
                Needs.Add(new NeedData(this, NeedType.CraftGood, item) { NeedCoreType = NeedCoreType.Building });

                // Add Need for "I need resources to craft"
                // TODO: If two craftable items use the same resource, then we'll have two needs for that resource.  sum up counts
                foreach (var resource in item.ResourcesNeededForCrafting)
                    CraftingResourceNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource.Item, resource.Count));
            }
            Needs.AddRange(CraftingResourceNeeds);
        }

        if (Defn.CanStoreItems)
        {
            ClearOutStorageNeed = new NeedData(this, NeedType.ClearStorage) { NeedCoreType = NeedCoreType.Building };
            Needs.Add(ClearOutStorageNeed);
        }

        if (Defn.CanSellGoods)
        {
            foreach (var item in Defn.GoodsThatCanBeSold)
            {
                // add need to sell (that our assigned sellers can fulfill)
                Needs.Add(new NeedData(this, NeedType.SellGood, item));

                // add need for items to sell (that other buildings can fulfill)
                var needForItemToSell = new NeedData(this, NeedType.CraftingOrConstructionMaterial, item, NumStorageSpots);
                SellingGoodNeeds.Add(needForItemToSell);
                Needs.Add(needForItemToSell);
            }
        }
    }

    internal void GetAvailableTasksForWorker(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        BuildingTaskMgr.GetAvailableTasksForWorker(availableTasks, worker, allTownNeeds);
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

    // public ItemData GetUnreservedItemOfType(ItemDefn itemDefn, ItemClass itemClass = ItemClass.Unset)
    // {
    //     var spot = GetStorageSpotWithUnreservedItemOfType(itemDefn, itemClass);
    //     return spot == null ? null : spot.ItemInStorage;
    // }

    public StorageSpotData GetStorageSpotWithUnreservedItemOfType(ItemDefn itemDefn, ItemClass itemClass = ItemClass.Unset)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInSpot != null && spot.ItemInSpot.DefnId == itemDefn.Id)// && spot.ItemInStorage.Defn.ItemClass == itemClass)
                    return spot;

        return null;
    }

    public bool HasUnreservedItemOfType(ItemDefn itemDefn)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInSpot != null && spot.ItemInSpot.DefnId == itemDefn.Id)
                    return true;

        return false;
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

        // grow
        foreach (var spot in GatheringSpots)
            spot.Update();
    }

    public void UpdateNeedPriorities()
    {
        // Hardcoded 9 storage spots per storage area
        float percentFull = 1 - (float)(NumAvailableStorageSpots) / (Defn.NumStorageAreas * Defn.StorageAreaSize.x * Defn.StorageAreaSize.y);

        if (Defn.CanStoreItems)
        {
            if (Defn.IsPrimaryStorage)
                ClearOutStorageNeed.Priority = 0;
            else
            {
                // unless close to full, Cleanup tasks are lower priority than filling crafting need tasks
                ClearOutStorageNeed.Priority = percentFull / 10f;

                // if we're a crafting building then we have a higher priority than non-crafting buildings (e.g. woodcutter) to clear storage
                // so that we can craft more
                if (Defn.CanCraft)
                    ClearOutStorageNeed.Priority *= 1.5f;
            }
        }

        foreach (var need in Needs)
        {
            if (need.Type == NeedType.CraftGood)
            {
                if (need.IsBeingFullyMet || IsPaused)
                {
                    need.Priority = 0;
                    continue;
                }

                // Priority of Crafting is set by:
                //  does anyone else need it and how badly (priority of need)
                //  how many of the item are already in the Town?
                //  do we want/need to sell the item?
                var globalPriorityOfNeedForItem = 0f;
                foreach (var building in Town.Buildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == need.NeededItem)
                            globalPriorityOfNeedForItem += otherNeed.Priority;

                var storageImpact = Mathf.Clamp(Town.NumTotalItemsInStorage(need.NeededItem) / 10f, 0, 2);

                var priorityToSellItem = 0f;
                foreach (var building in Town.Buildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.SellGood && otherNeed.NeededItem == need.NeededItem)
                            priorityToSellItem += otherNeed.Priority;

                need.Priority = storageImpact / 2f + globalPriorityOfNeedForItem + priorityToSellItem + .3f;
            }

            if (need.Type == NeedType.SellGood)
            {
                if (need.IsBeingFullyMet || IsPaused)
                {
                    need.Priority = 0;
                    continue;
                }
                var item = need.NeededItem;
                var globalNeedForItem = 0f;

                // if the item-to-be-sold is highly needed by other buildings, then don't sell it
                foreach (var building in Town.Buildings)
                {
                    foreach (var otherNeed in building.Needs)
                    {
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == item)
                            globalNeedForItem += otherNeed.Priority;
                        // if (otherNeed.Type == NeedType.ClearStorage && building.NumItemsInStorage(item) > 0) // todo: not quite right; only 1 of need's item is in storage will be the smae priority as if 9 of needs' item are in storage
                        // globalNeedForItem += otherNeed.Priority;
                        //   numInStorage += building.NumItemsInStorage(item); // doesn't include ferrying items but :shrug:
                    }
                }
                if (globalNeedForItem > 0.25f) // TODO: Allow user to modify this to e.g. effect a 'fire sale' in which even highly needed items are sold
                {
                    need.Priority = 0;
                    continue;
                }

                // if here then the item-to-be-sold isn't highly needed.  If there's a lot of it in storage, then sell it
                int numInStorage = Town.NumTotalItemsInStorage(item);
                var storageImpact = Mathf.Clamp(numInStorage / 10f, 0, 2);
                need.Priority = storageImpact / 2f + .2f;
            }
        }
        foreach (var need in GatheringNeeds)
        {
            if (IsPaused)
            {
                need.Priority = 0;
                continue;
            }
            need.Priority = .1f;
            // if we have a lot of them then reduce priority
            int numOfNeededItemAlreadyInStorage = NumItemsInStorage(need.NeededItem);
            numOfNeededItemAlreadyInStorage = Town.NumTotalItemsInStorage(need.NeededItem);

            if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 4)
                need.Priority *= .75f; // storage is 25%-50% full of the needed item
        }

        foreach (var need in CraftingResourceNeeds)
        {
            if (IsPaused)
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
        foreach (var need in SellingGoodNeeds)
        {
            if (IsPaused)
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
    }

    public bool HasUnreservedResourcesInStorageToCraftItem(ItemDefn item)
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

    public int NumItemsInStorage(ItemDefn itemDefn = null)
    {
        // TODO (perf): Dictionary lookup
        int count = 0;
        foreach (var area in StorageAreas)
            count += area.NumItemsInStorage(itemDefn);
        return count;
    }

    public void AddItemToItemSpot(ItemData item, ItemSpotData spot)
    {
        spot.AddItem(item);
    }

    public StorageSpotData GetEmptyStorageSpot()
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (spot.IsEmptyAndAvailable)
                    return spot;
        return null;
    }

    public StorageSpotData GetClosestEmptyStorageSpot(Vector3 worldLoc) => GetClosestEmptyStorageSpot(worldLoc, out float _);
    public StorageSpotData GetClosestEmptyStorageSpot(Vector3 worldLoc, out float dist)
    {
        StorageSpotData closestSpot = null;
        dist = float.MaxValue;
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (spot.IsEmptyAndAvailable)
                {
                    if (Vector3.Distance(worldLoc, spot.WorldLoc) < dist)
                    {
                        dist = Vector3.Distance(worldLoc, spot.WorldLoc);
                        closestSpot = spot;
                    }
                }
        return closestSpot;
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

    internal GatheringSpotData GetClosestGatheringSpot(Vector3 worldLoc, out float distance)
    {
        GatheringSpotData closestSpot = null;
        distance = float.MaxValue;

        // Find Closest unreserved gathering spot
        foreach (var spot in GatheringSpots)
            if (!spot.IsReserved)
            {
                var distToSpot = Vector3.Distance(worldLoc, spot.WorldLoc);
                if (distToSpot < distance)
                {
                    distance = distToSpot;
                    closestSpot = spot;
                }
            }
        if (closestSpot != null)
            return closestSpot;
        return null;
    }

    internal GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(Vector3 worldLoc, out float distance)
    {
        GatheringSpotData closestSpot = null;
        distance = float.MaxValue;

        // Find Closest unreserved gathering spot that has an item that needs to be gathered
        foreach (var spot in GatheringSpots)
            if (!spot.IsReserved && spot.ItemInSpot != null)
            {
                var distToSpot = Vector3.Distance(worldLoc, spot.WorldLoc);
                if (distToSpot < distance)
                {
                    distance = distToSpot;
                    closestSpot = spot;
                }
            }
        if (closestSpot != null)
            return closestSpot;
        return null;
    }

    internal StorageSpotData GetClosestUnreservedStorageSpotWithItemToReap(Vector3 worldLoc, out float distance)
    {
        StorageSpotData closestSpot = null;
        distance = float.MaxValue;

        // Find Closest unreserved gathering spot that has an item that needs to be gathered
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInSpot != null)
                {
                    var distToSpot = Vector3.Distance(worldLoc, spot.WorldLoc);
                    if (distToSpot < distance)
                    {
                        distance = distToSpot;
                        closestSpot = spot;
                    }
                }
        if (closestSpot != null)
            return closestSpot;
        return null;
    }

    internal GatheringSpotData ReserveClosestGatheringSpot(WorkerData worker, Vector3 worldLoc)
    {
        GatheringSpotData closestSpot = null;
        float dist = float.MaxValue;

        // Find Closest unreserved gathering spot
        foreach (var spot in GatheringSpots)
            if (!spot.IsReserved)
            {
                var distToSpot = Vector3.Distance(worldLoc, spot.WorldLoc);
                if (distToSpot < dist)
                {
                    dist = distToSpot;
                    closestSpot = spot;
                }
            }
        if (closestSpot != null)
        {
            closestSpot.ReserveBy(worker);
            return closestSpot;
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

    internal StorageSpotData ReserveStorageSpotClosestToWorldLoc(WorkerData worker, Vector3 worldLoc)
    {
        Debug.Assert(HasAvailableStorageSpot, "Assigning too many workers to storage spots");
        var spot = GetClosestEmptyStorageSpot(worldLoc);
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
                if (!spot.IsReserved && spot.ItemInSpot != null && spot.ItemInSpot.DefnId == item.Id)
                    return spot.ItemInSpot;
        return null;
    }

    internal StorageSpotData GetStorageSpotWithUnreservedItem(ItemDefn item)
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInSpot != null && spot.ItemInSpot.DefnId == item.Id)
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
                    Town.AddItemToGround(spot.ItemInSpot, spot.WorldLoc);// + WorldLoc);
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

    public void UpdateDistanceToOtherBuildings()
    {
        OtherBuildingsByDistance.Clear();
        foreach (var building in Town.Buildings)
        {
            float distance = Vector3.Distance(WorldLoc, building.WorldLoc);
            if (distance < int.MaxValue)
                OtherBuildingsByDistance.Add(new() { Building = building, Distance = distance });
        }
        OtherBuildingsByDistance.Sort((a, b) => (int)(a.Distance - b.Distance));
    }

    internal StorageSpotData GetFirstStorageSpotWithUnreservedItemToRemove()
    {
        foreach (var area in StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsReserved && spot.ItemInSpot != null)
                {
                    // don't allow returning resources that we need for crafting or selling
                    if (CraftingResourceNeeds.Find(need => need.NeededItem == spot.ItemInSpot.Defn) != null ||
                        SellingGoodNeeds.Find(need => need.NeededItem == spot.ItemInSpot.Defn) != null)
                        continue;
                    return spot;
                }
        return null;
    }

    internal CraftingSpotData GetAvailableCraftingSpot()
    {
        foreach (var spot in CraftingSpots)
            if (!spot.IsReserved)
                return spot;
        return null;
    }

    internal void TogglePaused()
    {
        Debug.Assert(Defn.PlayerCanPause, "Toggling paused on building that can't be paused");
        IsPaused = !IsPaused;
        foreach (var worker in Town.Workers)
            worker.OnBuildingPauseToggled(this);
    }
}
