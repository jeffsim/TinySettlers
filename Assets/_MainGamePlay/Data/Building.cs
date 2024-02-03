using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ConstructionState { NotStarted, UnderConstruction, FullyConstructed };

public delegate void OnLocationChangedEvent();

[Serializable]
public class DistanceToBuilding
{
    public float Distance;
    public BuildingData Building;
}

[Serializable]
public class BuildingData : BaseData, ILocationProvider
{
    public override string ToString() => Defn.FriendlyName + " (" + InstanceId + ")";

    private BuildingDefn _defn;
    public BuildingDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.BuildingDefns[DefnId];
    public string DefnId;

    // The Town which this Building is in
    public TownData Town;

    public bool IsDestroyed;

    // Which Tile the Building is in
    public int TileX;
    public int TileY;

    static float TileSize = 10;

    public bool IsPaused;

    [SerializeField] public LocationComponent Location { get; set; }

    [NonSerialized] public OnLocationChangedEvent OnLocationChanged;

    // public ConstructionState ConstructionState;
    // public float PercentBuilt; 
    // public bool IsConstructed => !(Defn.CanBeConstructed) || (ConstructionState == ConstructionState.FullyConstructed);

    public List<DistanceToBuilding> OtherBuildingsByDistance = new();

    public List<NeedData> Needs;

    public List<GatheringSpotData> GatheringSpots;
    public List<CraftingSpotData> CraftingSpots;

    // Storage related fields
    public List<StorageAreaData> StorageAreas;
    public int NumAvailableStorageSpots => StorageAreas.Sum(area => area.NumAvailableSpots);
    public int NumStorageSpots => StorageAreas.Sum(area => area.NumStorageSpots);
    public bool HasAvailableStorageSpot => StorageAreas.Any(area => area.HasAvailableSpot);
    public bool IsStorageFull => NumAvailableStorageSpots == 0;
    [SerializeReference] public List<StorageSpotData> StorageSpots;

    // Resource gathering fields
    public int NumReservedGatheringSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in GatheringSpots) if (spot.Reservation.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableGatheringSpots => Defn.GatheringSpots.Count - NumReservedGatheringSpots;
    public bool HasAvailableGatheringSpot => NumAvailableGatheringSpots > 0;

    // storage
    public int NumReservedStorageSpots => StorageAreas.Sum(area => area.NumReservedSpots);


    // Crafting fields
    public int NumReservedCraftingSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in CraftingSpots) if (spot.Reservation.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableCraftingSpots => Defn.CraftingSpots.Count - NumReservedCraftingSpots;
    public bool HasAvailableCraftingSpot => NumAvailableCraftingSpots > 0;

    // TODO: Track this in building instead of recalculating
    public int NumWorkers => Town.NumBuildingWorkers(this);

    // For easy tracking
    // public List<NeedData> ConstructionNeeds;

    // If this building can craft items or sell items, then ItemNeeds contains the priority of
    // how much we need each of those items.  priority is dependent on how many are in storage.
    public List<NeedData> ItemNeeds;

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
        Location = new(TileX * TileSize, TileY * TileSize);
    }

    public void Initialize(TownData town)
    {
        Town = town;

        GatheringSpots = new();
        for (int i = 0; i < Defn.GatheringSpots.Count; i++)
            GatheringSpots.Add(new(this, i));

        CraftingSpots = new();
        for (int i = 0; i < Defn.CraftingSpots.Count; i++)
            CraftingSpots.Add(new(this, i));

        StorageAreas = new();
        foreach (var areaDefn in Defn.StorageAreas)
            StorageAreas.Add(new(this, areaDefn));

        // Create a unified list of storage spots since I don't want to iteratve over all areas.piles.spots every time
        StorageSpots = new();
        foreach (var area in StorageAreas)
            foreach (var pile in area.StoragePiles)
                StorageSpots.AddRange(pile.StorageSpots);

        Needs = new List<NeedData>();
        // ConstructionNeeds = new List<NeedData>();
        GatheringNeeds = new();
        ItemNeeds = new();

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

        if (Defn.CanStoreItems)
        {
            ClearOutStorageNeed = new NeedData(this, NeedType.ClearStorage) { NeedCoreType = NeedCoreType.Building };
            Needs.Add(ClearOutStorageNeed);
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
                    ItemNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource.Item, resource.Count));
            }
            Needs.AddRange(ItemNeeds);
        }

        if (Defn.CanSellGoods)
        {
            foreach (var item in Defn.GoodsThatCanBeSold)
            {
                // add need to sell (that our assigned sellers can fulfill)
                Needs.Add(new NeedData(this, NeedType.SellItem, item));

                // add need for items to sell (that other buildings can fulfill)
                var needForItemToSell = new NeedData(this, NeedType.CraftingOrConstructionMaterial, item, NumStorageSpots);
                ItemNeeds.Add(needForItemToSell);
            }
            Needs.AddRange(ItemNeeds);
        }
        UpdateWorldLoc();
    }

    public int GetStorageSpotIndex(StorageSpotData spotToCheck)
    {
        Debug.Assert(spotToCheck.Building == this, "wrong building");
        var index = 0;
        foreach (var spot in StorageSpots)
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
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null && spot.ItemContainer.Item.DefnId == itemDefn.Id)// && spot.ItemInStorage.Defn.ItemClass == itemClass)
                return spot;

        return null;
    }

    public bool HasUnreservedItemOfType(ItemDefn itemDefn)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null && spot.ItemContainer.Item.DefnId == itemDefn.Id)
                return true;

        return false;
    }

    /**
        returns true if this building supports gathering the required resource AND there's
        an available gathering spot
*/
    public bool ResourceCanBeGatheredFromHere(ItemDefn itemDefn)
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
        int totalNumStorageSpot = StorageAreas.Sum(area => area.NumStorageSpots);

        float percentFull = 1 - (float)NumAvailableStorageSpots / totalNumStorageSpot;

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
                        if (otherNeed.Type == NeedType.SellItem && otherNeed.NeededItem == need.NeededItem)
                            priorityToSellItem += otherNeed.Priority;

                need.Priority = storageImpact / 2f + globalPriorityOfNeedForItem + priorityToSellItem + .3f;
            }

            if (need.Type == NeedType.SellItem)
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
                    if (building == this) continue;
                    foreach (var otherNeed in building.Needs)
                    {
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == item)
                            globalNeedForItem += otherNeed.Priority;
                        // if (otherNeed.Type == NeedType.ClearStorage && building.NumItemsInStorage(item) > 0) // todo: not quite right; only 1 of need's item is in storage will be the smae priority as if 9 of needs' item are in storage
                        // globalNeedForItem += otherNeed.Priority;
                        //   numInStorage += building.NumItemsInStorage(item); // doesn't include ferrying items but :shrug:
                    }
                }
                if (globalNeedForItem > 0.5f) // TODO: Allow user to modify this to e.g. effect a 'fire sale' in which even highly needed items are sold
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

        foreach (var need in ItemNeeds)
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
            else if (numOfNeededItemAlreadyInStorage > totalNumStorageSpot / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > totalNumStorageSpot / 4)
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

    public StorageSpotData GetEmptyStorageSpot() => StorageSpots.First(spot => spot.IsEmptyAndAvailable);

    public StorageSpotData GetClosestEmptyStorageSpot(LocationComponent loc) => GetClosestEmptyStorageSpot(loc, out float _);
    public StorageSpotData GetClosestEmptyStorageSpot(LocationComponent loc, out float dist)
    {
        return loc.GetClosest(StorageSpots, out dist, spot => spot.IsEmptyAndAvailable);
    }

    public StorageSpotData GetClosestUnreservedStorageSpotWithItem(LocationComponent loc, ItemDefn itemDefn) => GetClosestUnreservedStorageSpotWithItem(loc, itemDefn, out float _);
    public StorageSpotData GetClosestUnreservedStorageSpotWithItem(LocationComponent loc, ItemDefn itemDefn, out float distance)
    {
        return loc.GetClosest(StorageSpots, out distance, spot => !spot.Reservation.IsReserved && spot.ItemContainer.ContainsItem(itemDefn));
    }

    public StorageSpotData GetClosestUnreservedStorageSpotWithItemToReapOrSell(LocationComponent loc) => GetClosestUnreservedStorageSpotWithItemToReapOrSell(loc, out float _);
    public StorageSpotData GetClosestUnreservedStorageSpotWithItemToReapOrSell(LocationComponent loc, out float distance)
    {
        return loc.GetClosest(StorageSpots, out distance, spot => !spot.Reservation.IsReserved && spot.ItemContainer.HasItem);
    }

    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(LocationComponent loc) => GetClosestUnreservedGatheringSpotWithItemToReap(loc, out float _);
    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(LocationComponent loc, out float distance)
    {
        return loc.GetClosest(GatheringSpots, out distance, spot => !spot.Reservation.IsReserved && spot.ItemContainer.HasItem);
    }

    public CraftingSpotData ReserveCraftingSpot(WorkerData worker) => worker.ReserveFirstReservable(CraftingSpots);
    public StorageSpotData ReserveStorageSpot(WorkerData worker) => worker.ReserveFirstReservable(StorageSpots);
    public GatheringSpotData ReserveGatheringSpot(WorkerData worker) => worker.ReserveFirstReservable(GatheringSpots);

    public void UnreserveCraftingSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(CraftingSpots);
    public void UnreserveStorageSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(StorageSpots);
    public void UnreserveGatheringSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(GatheringSpots);

    public GatheringSpotData ReserveClosestGatheringSpot(WorkerData worker, LocationComponent loc)
    {
        var spot = loc.GetClosest(GatheringSpots);
        spot?.Reservation.ReserveBy(worker);
        return spot;
    }

    public ItemData GetUnreservedItemInStorage(ItemDefn item)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null && spot.ItemContainer.Item.DefnId == item.Id)
                return spot.ItemContainer.Item;
        return null;
    }

    public StorageSpotData GetStorageSpotWithUnreservedItem(ItemDefn item)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null && spot.ItemContainer.Item.DefnId == item.Id)
                return spot;
        return null;
    }

    public void Debug_RemoveAllItemsFromStorage()
    {
        foreach (var area in StorageAreas)
            area.Debug_RemoveAllItemsFromStorage();
    }

    public void Destroy()
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");
        Debug.Assert(this != Town.Camp, "Can't destroy camp");

        IsDestroyed = true;

        foreach (var need in Needs) need.Cancel();

        foreach (var worker in Town.Workers) worker.OnBuildingDestroyed(this);
        foreach (var spot in CraftingSpots) spot.OnBuildingDestroyed();
        foreach (var spot in GatheringSpots) spot.OnBuildingDestroyed();
        foreach (var area in StorageAreas) area.OnBuildingDestroyed();
    }

    public void MoveTo(int tileX, int tileY)
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");

        TileX = tileX;
        TileY = tileY;

        LocationComponent previousWorldLoc = new(Location);
        Location.SetWorldLoc(TileX * TileSize, TileY * TileSize);
        UpdateWorldLoc();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.Workers)
            worker.OnBuildingMoved(this, previousWorldLoc);

        OnLocationChanged?.Invoke();
    }

    public void UpdateWorldLoc()
    {
        // TODO: UGH
        foreach (var area in StorageAreas)
            area.UpdateWorldLoc();
        foreach (var spot in CraftingSpots)
            spot.Location.UpdateWorldLoc();
        foreach (var spot in GatheringSpots)
            spot.Location.UpdateWorldLoc();
    }

    public void UpdateDistanceToOtherBuildings()
    {
        OtherBuildingsByDistance.Clear();
        foreach (var building in Town.Buildings)
        {
            float distance = Location.DistanceTo(building.Location);
            if (distance < int.MaxValue)
                OtherBuildingsByDistance.Add(new() { Building = building, Distance = distance });
        }
        OtherBuildingsByDistance.Sort((a, b) => (int)(a.Distance - b.Distance));
    }

    public StorageSpotData GetFirstStorageSpotWithUnreservedItemToRemove()
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null)
            {
                // Allow returning resources that we need for crafting or selling if we're paused or have no workers assigned
                var allowRemovingNeededItems = IsPaused || NumWorkers == 0;
                if (!allowRemovingNeededItems && ItemNeeds.Find(need => need.NeededItem == spot.ItemContainer.Item.Defn) != null)
                    continue;
                return spot;
            }
        return null;
    }

    public StorageSpotData GetClosestStorageSpotWithUnreservedItemToRemove(LocationComponent location)
    {
        StorageSpotData closestSpot = null;
        float closestDist = float.MaxValue;
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null)
            {
                // Allow returning resources that we need for crafting or selling if we're paused or have no workers assigned
                var allowRemovingNeededItems = IsPaused || NumWorkers == 0;
                if (!allowRemovingNeededItems && ItemNeeds.Find(need => need.NeededItem == spot.ItemContainer.Item.Defn) != null)
                    continue;

                var dist = location.DistanceTo(spot.Location);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestSpot = spot;
                }
            }
        return closestSpot;
    }

    public List<StorageSpotData> GetStorageSpotsWithUnreservedItem(ItemDefn itemDefn)
    {
        var spots = new List<StorageSpotData>();
        foreach (var spot in StorageSpots)
            if (!spot.Reservation.IsReserved && spot.ItemContainer.Item != null && spot.ItemContainer.Item.DefnId == itemDefn.Id)
                spots.Add(spot);
        return spots;
    }

    public CraftingSpotData GetAvailableCraftingSpot()
    {
        foreach (var spot in CraftingSpots)
            if (!spot.Reservation.IsReserved)
                return spot;
        return null;
    }

    public void TogglePaused()
    {
        Debug.Assert(Defn.PlayerCanPause, "Toggling paused on building that can't be paused");
        IsPaused = !IsPaused;
        foreach (var worker in Town.Workers)
            worker.OnBuildingPauseToggled(this);
    }
}
