using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void OnLocationChangedEvent();

[Serializable]
public class BuildingData : BaseData, ILocation, IOccupiable, IConstructable, IPausable
{
    public override string ToString() => Defn.FriendlyName + " (" + InstanceId + ")";

    private BuildingDefn _defn;
    public BuildingDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.BuildingDefns[DefnId];
    public string DefnId;

    // The Town which this Building is in
    public TownData Town;

#if UNITY_INCLUDE_TESTS
    public string TestId;
#endif

    public bool IsDestroyed;

    // Which Tile the Building is in
    public int TileX;
    public int TileY;

    static float TileSize = 10;

    public BuildingCraftingMgr CraftingMgr;

    public List<NeedData> ConstructionNeeds = new();

    // Data Components
    [SerializeField] public Constructable Constructable { get; set; }
    [SerializeField] public Location Location { get; set; }
    [SerializeField] public Occupiable Occupiable { get; set; }
    [SerializeField] public Pausable Pausable { get; set; }

    [NonSerialized] public OnLocationChangedEvent OnLocationChanged;

    // Accessors
    public bool IsPaused => Pausable.IsPaused;

    public List<NeedData> Needs = new();

    public List<GatheringSpotData> GatheringSpots = new();
    public List<SleepingSpotData> SleepingSpots = new();

    // Storage related fields
    public List<StorageAreaData> StorageAreas = new();
    public int NumAvailableStorageSpots => StorageAreas.Sum(area => area.NumAvailableSpots);
    public int NumStorageSpots => StorageAreas.Sum(area => area.NumStorageSpots);
    public bool HasAvailableStorageSpot => StorageAreas.Any(area => area.HasAvailableSpot);
    public bool IsStorageFull => NumAvailableStorageSpots == 0;
    [SerializeReference] public List<StorageSpotData> StorageSpots = new();

    // Resource gathering fields
    public int NumReservedGatheringSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in GatheringSpots) if (spot.Reservable.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableGatheringSpots => Defn.GatheringSpots.Count - NumReservedGatheringSpots;
    public bool HasAvailableGatheringSpot => NumAvailableGatheringSpots > 0;

    // storage
    public int NumReservedStorageSpots => StorageAreas.Sum(area => area.NumReservedSpots);

    // TODO: Track this in building instead of recalculating
    public int NumWorkers => Town.TownWorkerMgr.NumBuildingWorkers(this);

    // If this building can craft items or sell items, then ItemNeeds contains the priority of
    // how much we need each of those items.  priority is dependent on how many are in storage.
    public List<NeedData> ItemNeeds = new();

    // How badly we need a courier to clear out our storage
    public NeedData ClearOutStorageNeed;

    // If this building can gather resources, then GatheringNeeds contains the priority of
    // gathering each resource.  e.g.:
    // * if we have many of X and few of Y, then Y may have a higher priority
    // * if our storage is nearly full then all resource gathering is at a reduced priority
    // * TODO: If another building has broadcast a need for resource R and we can gather it, then
    //   increase priority to gather it.  note that a settlers-like model may ONLY do these.
    public List<NeedData> GatheringNeeds = new();

    public BuildingData(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        DefnId = buildingDefn.Id;
        TileX = tileX;
        TileY = tileY;
        Location = new(new(TileX * TileSize, Settings.Current.BuildingsY, TileY * TileSize));
    }

    public BuildingData(BuildingDefn buildingDefn, Vector3 worldLoc)
    {
        DefnId = buildingDefn.Id;
        Location = new(worldLoc);
    }

    public void Initialize(TownData town)
    {
        Town = town;

        Occupiable = new(Defn.Occupiable, this);
        Pausable = new(Defn.Pausable, this);
        Constructable = new(Defn.Constructable, this);

        if (Defn.CanCraft)
        {
            CraftingMgr = new(this);
        }

        if (Defn.ResourcesCanBeGatheredFromHere)
        {
            for (int i = 0; i < Defn.GatheringSpots.Count; i++)
                GatheringSpots.Add(new(this, i));
        }

        if (Defn.WorkersCanRestHere)
        {
            for (int i = 0; i < Defn.SleepingSpots.Count; i++)
                SleepingSpots.Add(new(this, i));
        }

        if (Defn.CanStoreItems)
        {
            foreach (var areaDefn in Defn.StorageAreas)
                StorageAreas.Add(new(this, areaDefn));
        }

        // Create a unified list of storage spots since I don't want to iteratve over all areas.piles.spots every time
        foreach (var area in StorageAreas)
            foreach (var pile in area.StoragePiles)
                StorageSpots.AddRange(pile.StorageSpots);

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

        if (Defn.CanSellGoods)
        {
            foreach (var item in Defn.GoodsThatCanBeSold)
            {
                // add need to sell (that our assigned sellers can fulfill)
                Needs.Add(new NeedData(this, NeedType.SellItem, item, 1000));

                // add need for items to sell (that other buildings can fulfill)
                var needForItemToSell = new NeedData(this, NeedType.CraftingOrConstructionMaterial, item, NumStorageSpots);
                ItemNeeds.Add(needForItemToSell);
            }
            Needs.AddRange(ItemNeeds);
        }

        // Add need for construction and materials if the building needs to be constructed
        if (Defn.CanBeConstructed && !Constructable.IsConstructed)
        {
            // ConstructionNeeds.Add(new NeedData(this, NeedType.ConstructionWorker, null, Defn.NumConstructorSpots));
            // foreach (var resource in Defn.ResourcesNeededForConstruction)
            //     ConstructionNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource));
            // Needs.AddRange(ConstructionNeeds);
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
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null && spot.Container.FirstItem.DefnId == itemDefn.Id)// && spot.ItemInStorage.Defn.ItemClass == itemClass)
                return spot;

        return null;
    }

    public bool NeedsItemForSelf(ItemDefn itemDefn) => ItemNeeds.Find(need => need.NeededItem == itemDefn) != null;
    public bool HasUnreservedItemOfTypeAndDoesntNeedIt(ItemDefn itemDefn) => StorageSpots.Any(spot => !spot.Reservable.IsReserved && spot.Container.ContainsItem(itemDefn) && !NeedsItemForSelf(itemDefn));

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

        foreach (var need in ItemNeeds)
        {
            if (IsPaused)
            {
                need.Priority = 0;
                continue;
            }
            need.Priority = 1;
            float storageOccupancyRatio = (float)NumItemsInStorage / totalNumStorageSpot;
            float fullnessAdjustment = percentFull > 0.99f ? 0 :
                                       percentFull > 0.75f ? 0.5f :
                                       percentFull > 0.5f ? 0.75f :
                                       percentFull > 0.25f ? 0.875f :
                                       percentFull > 0.1f ? 0.9f : 1f;
            float occupancyAdjustment = storageOccupancyRatio > 0.5f ? 0.5f :
                                        storageOccupancyRatio > 0.25f ? 0.75f : 1f;

            need.Priority *= Math.Min(fullnessAdjustment, occupancyAdjustment);
        }

        foreach (var need in Needs)
        {
            if (need.IsBeingFullyMet || IsPaused)
            {
                need.Priority = 0;
                continue;
            }

            if (need.Type == NeedType.CraftGood)
            {
                // Priority of Crafting is set by:
                //  does anyone else need it and how badly (priority of need)
                //  how many of the item are already in the Town?
                //  do we want/need to sell the item?
                var globalPriorityOfNeedForItem = 0f;
                foreach (var building in Town.AllBuildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == need.NeededItem)
                            globalPriorityOfNeedForItem += otherNeed.Priority;

                var storageImpact = Mathf.Clamp(Town.NumTotalItemsInStorage(need.NeededItem) / 10f, 0, 2);

                var priorityToSellItem = 0f;
                foreach (var building in Town.AllBuildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.SellItem && otherNeed.NeededItem == need.NeededItem)
                            priorityToSellItem += otherNeed.Priority;

                need.Priority = storageImpact / 2f + globalPriorityOfNeedForItem + priorityToSellItem + .3f;
                need.Priority = 2;
            }

            if (need.Type == NeedType.SellItem)
            {
                // The need to sell (for the worker) is always 1.  Howver, the building's need for the item
                // to be sold is based on how many are in storage and how badly other buildings need it
                need.Priority = 2;

                // Get the ItemNeed for this item to sell
                var itemNeed = ItemNeeds.Find(n => n.NeededItem == need.NeededItem);
                Debug.Assert(itemNeed != null, "ItemNeed not found for item to sell");

                var item = itemNeed.NeededItem;
                var globalNeedForItem = 0f;

                // if the item-to-be-sold is highly needed by other buildings, then don't sell it
                foreach (var building in Town.AllBuildings)
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
                // if (globalNeedForItem > 0.5f) // TODO: Allow user to modify this to e.g. effect a 'fire sale' in which even highly needed items are sold
                // {
                //     need.Priority = 0;
                //     continue;
                // }

                // if here then the item-to-be-sold isn't highly needed.  If there's a lot of it in storage, then sell it
                int numInStorage = Town.NumTotalItemsInStorage(item);
                var storageImpact = Mathf.Clamp(numInStorage / 10f, 0, 2);
                itemNeed.Priority = storageImpact / 2f + .2f;
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
            int numOfNeededItemAlreadyInStorage = NumItemsOfTypeInStorage(need.NeededItem);
            numOfNeededItemAlreadyInStorage = Town.NumTotalItemsInStorage(need.NeededItem);

            if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 4)
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
            num += area.NumUnreservedItemsOfTypeInStorage(itemDefn);
        return num >= count;
    }

    public int NumItemsInStorage => StorageAreas.Sum(area => area.NumItemsInStorage);
    public int NumItemsOfTypeInStorage(ItemDefn itemDefn) => StorageAreas.Sum(area => area.NumItemsOfTypeInStorage(itemDefn));

    public StorageSpotData GetEmptyStorageSpot() => StorageSpots.First(spot => spot.Container.IsEmpty && !spot.Reservable.IsReserved);

    public StorageSpotData GetClosestEmptyStorageSpot(Location loc) => GetClosestEmptyStorageSpot(loc, out float _);
    public StorageSpotData GetClosestEmptyStorageSpot(Location loc, out float dist)
    {
        return loc.GetClosest(StorageSpots, out dist, spot => spot.Container.IsEmpty && !spot.Reservable.IsReserved);
    }

    public StorageSpotData GetClosestUnreservedStorageSpotWithItem(Location loc, ItemDefn itemDefn) => GetClosestUnreservedStorageSpotWithItem(loc, itemDefn, out float _);
    public StorageSpotData GetClosestUnreservedStorageSpotWithItem(Location loc, ItemDefn itemDefn, out float distance)
    {
        return loc.GetClosest(StorageSpots, out distance, spot => !spot.Reservable.IsReserved && spot.Container.ContainsItem(itemDefn));
    }

    public StorageSpotData GetClosestUnreservedStorageSpotWithItemIgnoreList(Location loc, ItemDefn itemDefn, List<StorageSpotData> ignore) => GetClosestUnreservedStorageSpotWithItemIgnoreList(loc, itemDefn, ignore, out float _);
    public StorageSpotData GetClosestUnreservedStorageSpotWithItemIgnoreList(Location loc, ItemDefn itemDefn, List<StorageSpotData> ignore, out float distance)
    {
        return loc.GetClosest(StorageSpots, out distance, spot => !spot.Reservable.IsReserved && spot.Container.ContainsItem(itemDefn) && !ignore.Contains(spot));
    }

    public StorageSpotData GetClosestUnreservedStorageSpotWithItemToReapOrSell(Location loc) => GetClosestUnreservedStorageSpotWithItemToReapOrSell(loc, out float _);
    public StorageSpotData GetClosestUnreservedStorageSpotWithItemToReapOrSell(Location loc, out float distance)
    {
        return loc.GetClosest(StorageSpots, out distance, spot => !spot.Reservable.IsReserved && spot.Container.HasItem);
    }

    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(Location loc) => GetClosestUnreservedGatheringSpotWithItemToReap(loc, out float _);
    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(Location loc, out float distance)
    {
        return loc.GetClosest(GatheringSpots, out distance, spot => !spot.Reservable.IsReserved && spot.Container.HasItem);
    }

    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(Location loc, ItemDefn itemDefn) => GetClosestUnreservedGatheringSpotWithItemToReap(loc, itemDefn, out float _);
    public GatheringSpotData GetClosestUnreservedGatheringSpotWithItemToReap(Location loc, ItemDefn itemDefn, out float distance)
    {
        return loc.GetClosest(GatheringSpots, out distance, spot => !spot.Reservable.IsReserved && spot.Container.HasItem && spot.Container.FirstItem.DefnId == itemDefn.Id);
    }

    public StorageSpotData ReserveStorageSpot(WorkerData worker) => worker.ReserveFirstReservable(StorageSpots);
    public GatheringSpotData ReserveGatheringSpot(WorkerData worker) => worker.ReserveFirstReservable(GatheringSpots);

    public void UnreserveStorageSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(StorageSpots);
    public void UnreserveGatheringSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(GatheringSpots);

    public GatheringSpotData ReserveClosestGatheringSpot(WorkerData worker, Location loc)
    {
        var spot = loc.GetClosest(GatheringSpots);
        spot?.Reservable.ReserveBy(worker);
        return spot;
    }

    public ItemData GetUnreservedItemInStorage(ItemDefn item)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null && spot.Container.FirstItem.DefnId == item.Id)
                return spot.Container.FirstItem;
        return null;
    }

    public StorageSpotData GetStorageSpotWithUnreservedItem(ItemDefn item)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null && spot.Container.FirstItem.DefnId == item.Id)
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

        Occupiable?.EvictAllOccupants();
        foreach (var need in Needs) need.Cancel();
        foreach (var worker in Town.TownWorkerMgr.Workers) worker.OnBuildingDestroyed(this);
        foreach (var spot in GatheringSpots) spot.OnBuildingDestroyed();
        foreach (var area in StorageAreas) area.OnBuildingDestroyed();
        CraftingMgr?.Destroy();
    }

    public void MoveTo(Vector3 worldLoc)
    {
        Location previousWorldLoc = new(Location.WorldLoc);
        Location.SetWorldLoc(worldLoc);
        UpdateWorldLoc();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.TownWorkerMgr.Workers)
            worker.OnBuildingMoved(this, previousWorldLoc);

        OnLocationChanged?.Invoke();
    }

    public void MoveTo(int tileX, int tileY)
    {
        Debug.Assert(!IsDestroyed, "destroying building twice");

        TileX = tileX;
        TileY = tileY;

        Location previousWorldLoc = new(Location.WorldLoc);
        Location.SetWorldLoc(TileX * TileSize, Location.WorldLoc.y, TileY * TileSize);
        UpdateWorldLoc();

        // Update Workers that are assigned to or have Tasks which involve this building.
        foreach (var worker in Town.TownWorkerMgr.Workers)
            worker.OnBuildingMoved(this, previousWorldLoc);

        OnLocationChanged?.Invoke();
    }

    public void UpdateWorldLoc()
    {
        // TODO: UGH
        foreach (var area in StorageAreas) area.UpdateWorldLoc();
        foreach (var spot in GatheringSpots) spot.UpdateWorldLoc();
        foreach (var spot in SleepingSpots) spot.UpdateWorldLoc();
        CraftingMgr?.UpdateWorldLoc();
    }

    public StorageSpotData GetFirstStorageSpotWithUnreservedItemToRemove()
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null)
            {
                // Allow returning resources that we need for crafting or selling if we're paused or have no workers assigned
                var allowRemovingNeededItems = IsPaused || NumWorkers == 0;
                if (!allowRemovingNeededItems && ItemNeeds.Find(need => need.NeededItem == spot.Container.FirstItem.Defn) != null)
                    continue;
                return spot;
            }
        return null;
    }

    public StorageSpotData GetClosestStorageSpotWithUnreservedItemToRemove(Location location)
    {
        StorageSpotData closestSpot = null;
        float closestDist = float.MaxValue;
        foreach (var spot in StorageSpots)
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null)
            {
                // Allow returning resources that we need for crafting or selling if we're paused or have no workers assigned
                var allowRemovingNeededItems = IsPaused || NumWorkers == 0;
                if (!allowRemovingNeededItems && ItemNeeds.Find(need => need.NeededItem == spot.Container.FirstItem.Defn) != null)
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
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null && spot.Container.FirstItem.DefnId == itemDefn.Id)
                spots.Add(spot);
        return spots;
    }

    public void OnPauseToggled()
    {
        foreach (var worker in Town.TownWorkerMgr.Workers)
            worker.OnBuildingPauseToggled(this);
    }
}
