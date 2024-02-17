using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void OnItemAddedToGroundEvent(ItemData item);

public enum StorageSpotSearchType { Any, Primary, AssignedBuildingOrPrimary }

[Serializable]
public class TownData : BaseData
{
    private TownDefn _defn;
    public TownDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.TownDefns[DefnId];

    public string DefnId;

    public int Gold;

    [NonSerialized] public OnItemAddedToGroundEvent OnItemAddedToGround;
    [NonSerialized] public Action<ItemData> OnItemRemovedFromGround;
    [NonSerialized] public Action<ItemData> OnItemSold;

    [NonSerialized] public Action<BuildingData> OnBuildingAdded;
    [NonSerialized] public Action<BuildingData> OnBuildingRemoved;

    public TownTaskMgr TownTaskMgr;
    public TownWorkerMgr TownWorkerMgr;
    public TownTimeMgr TimeMgr;

    public int NumHomedWorkers => Buildings.Sum(building => building.OccupantMgr == null ? 0 : building.OccupantMgr.NumOccupants);

    // Current Map
    public BuildingData Camp;
    public List<TileData> Tiles = new();
    public List<BuildingData> Buildings = new();
    public List<ItemData> ItemsOnGround = new();
    public List<NeedData> otherTownNeeds = new();

    public TownData(TownDefn townDefn)
    {
        DefnId = townDefn.Id;
    }

    public float lastGameTime;

    public void OnLoaded()
    {
        GameTime.time = lastGameTime;
        TownWorkerMgr.OnLoaded();
    }

    public void InitializeOnFirstEnter()
    {
        lastGameTime = GameTime.time = 0;
        Tiles.Clear();
        string[] tiles = Defn.Tiles.Split(",");
        Debug.Assert(tiles.Length == Defn.Width * Defn.Height, "wrong num tiles");
        for (int y = 0; y < Defn.Height; y++)
            for (int x = 0; x < Defn.Width; x++)
                Tiles.Add(new TileData(x, y, tiles[y * Defn.Width + x]));

        Gold = 0;
        TownWorkerMgr = new(this);
        TimeMgr = new();

        Buildings.Clear();
        otherTownNeeds.Clear();
        foreach (var tbDefn in Defn.Buildings)
        {
            if (!tbDefn.IsEnabled) continue;

            var building = ConstructBuilding(tbDefn.Building, tbDefn.TileX, tbDefn.TileY);
            if (building.Defn.BuildingClass == BuildingClass.Camp)
                Camp = building;
#if UNITY_INCLUDE_TESTS
            building.TestId = tbDefn.TestId;
#endif
            for (int i = 0; i < tbDefn.NumWorkersStartAtBuilding; i++)
                CreateWorkerInBuilding(building);

            foreach (var item in tbDefn.StartingItemsInBuilding)
                for (int i = 0; i < item.Count; i++)
                    building.GetEmptyStorageSpot().ItemContainer.SetItem(new ItemData() { DefnId = item.Item.Id });
        }
        TownTaskMgr = new(this);
    }

    public WorkerData CreateWorkerInBuilding(BuildingData building)
    {
        var worker = new WorkerData(building);
        TownWorkerMgr.AddWorker(worker);
        FindHomesForUnhomedWorkers();
        return worker;
    }

    internal void TestMoveBuilding(int test)
    {
        MoveBuilding(Buildings[test], 2, 1);
    }

    public BuildingData ConstructBuilding(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        var building = new BuildingData(buildingDefn, tileX, tileY);
        Tiles[tileY * Defn.Width + tileX].BuildingInTile = building;
        return internalConstructBuilding(building);
    }

    public BuildingData ConstructBuilding(BuildingDefn buildingDefn, Vector3 worldLoc)
    {
        return internalConstructBuilding(new BuildingData(buildingDefn, worldLoc));
    }

    private BuildingData internalConstructBuilding(BuildingData building)
    {
        building.Initialize(this);
        Buildings.Add(building);
        OnBuildingAdded?.Invoke(building);
        FindHomesForUnhomedWorkers();
        return building;
    }

    private void FindHomesForUnhomedWorkers()
    {
        foreach (var worker in TownWorkerMgr.Workers)
            if (!worker.Occupant.HasHome)
            {
                var availableHome = Buildings.FirstOrDefault(building => building.Defn.WorkersCanLiveHere && building.OccupantMgr.HasRoom);
                if (availableHome == null)
                    return; // no more homes with available space

                availableHome.OccupantMgr.AddOccupant(worker);
            }
    }

    public void DestroyBuilding(BuildingData building)
    {
        if (!Settings.AllowFreeBuildingPlacement)
            Tiles[building.TileY * Defn.Width + building.TileX].BuildingInTile = null;
        Buildings.Remove(building);
        building.Destroy();
        OnBuildingRemoved?.Invoke(building);
        FindHomesForUnhomedWorkers();
    }


    public void MoveBuilding(BuildingData building, Vector3 worldLoc)
    {
        building.MoveTo(worldLoc);
    }

    public void MoveBuilding(BuildingData building, int tileX, int tileY)
    {
        Tiles[building.TileY * Defn.Width + building.TileX].BuildingInTile = null;
        building.MoveTo(tileX, tileY);
        Tiles[tileY * Defn.Width + tileX].BuildingInTile = building;
    }

    public void Update()
    {
        GameTime.Update();
        TimeMgr.Update();

        foreach (var building in Buildings)
            building.Update();

        // e.g. pick up items on the ground
        updateTownNeeds();

        TownTaskMgr.AssignTaskToIdleWorker();
        TownWorkerMgr.Update();
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

    internal BuildingData GetNearestResourceSource(LocationComponent loc, ItemDefn itemDefn)
    {
        return loc.GetClosest(Buildings, building => building.ResourceCanBeGatheredFromHere(itemDefn));
    }

    internal void AddItemToGround(ItemData item, LocationComponent loc)
    {
        ItemsOnGround.Add(item);
        item.Location.SetWorldLoc(loc);
        OnItemAddedToGround?.Invoke(item);

        // Add a need to pick up the item.  This will be removed when the item is picked up
        otherTownNeeds.Add(NeedData.CreateAbandonedItemCleanupNeed(item));
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

    public bool HasAvailableStorageSpot(StorageSpotSearchType searchType = StorageSpotSearchType.Any, WorkerData worker = null) => GetAvailableStorageSpot(searchType, worker) != null;
    public bool HasAvailablePrimaryOrAssignedStorageSpot(WorkerData worker) => GetAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, worker) != null;

    public StorageSpotData GetAvailableStorageSpot(StorageSpotSearchType searchType, WorkerData worker = null)
    {
        Debug.Assert(searchType != StorageSpotSearchType.AssignedBuildingOrPrimary || worker != null, "worker must be specified for AnyAssignedBuildingOrPrimary");

        foreach (var building in Buildings)
        {
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused && !building.IsDestroyed)
            {
                var buildingMatchesSearchType = searchType switch
                {
                    StorageSpotSearchType.Any => true,
                    StorageSpotSearchType.Primary => building.Defn.IsPrimaryStorage,
                    StorageSpotSearchType.AssignedBuildingOrPrimary => building.Defn.IsPrimaryStorage || building == worker.Assignment.AssignedTo,
                    _ => throw new Exception("unhandled search type " + searchType),
                };

                if (buildingMatchesSearchType)
                {
                    var spot = building.GetEmptyStorageSpot();
                    if (spot != null)
                        return spot;
                }
            }
        }
        return null;
    }

    public StorageSpotData GetClosestAvailableStorageSpot(StorageSpotSearchType searchType, LocationComponent location, WorkerData worker = null) => GetClosestAvailableStorageSpot(searchType, location, worker, out float _);
    public StorageSpotData GetClosestAvailableStorageSpot(StorageSpotSearchType searchType, LocationComponent location, WorkerData worker, out float dist)
    {
        if (searchType == StorageSpotSearchType.AssignedBuildingOrPrimary)
            Debug.Assert(worker != null, "worker must be specified for AnyAssignedBuildingOrPrimary");

        BuildingData closestBuilding = null;
        dist = float.MaxValue;
        foreach (var building in Buildings)
        {
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused && !building.IsDestroyed)
            {
                var buildingMatchesSearchType = searchType switch
                {
                    StorageSpotSearchType.Any => true,
                    StorageSpotSearchType.Primary => building.Defn.IsPrimaryStorage,
                    StorageSpotSearchType.AssignedBuildingOrPrimary => building.Defn.IsPrimaryStorage || building == worker.Assignment.AssignedTo,
                    _ => throw new Exception("unhandled search type " + searchType),
                };

                if (buildingMatchesSearchType)
                {
                    var distanceToBuilding = location.DistanceTo(building.Location);
                    if (distanceToBuilding < dist)
                    {
                        closestBuilding = building;
                        dist = distanceToBuilding;
                    }
                }
            }
        }
        return closestBuilding?.GetClosestEmptyStorageSpot(location, out dist);
    }

    public GatheringSpotData GetClosestAvailableGatheringSpot(LocationComponent location, ItemDefn itemDefn, WorkerData worker = null) => GetClosestAvailableGatheringSpot(location, itemDefn, worker, out float _);
    public GatheringSpotData GetClosestAvailableGatheringSpot(LocationComponent location, ItemDefn itemDefn, WorkerData worker, out float dist)
    {
        BuildingData closestBuilding = null;
        dist = float.MaxValue;
        foreach (var building in Buildings)
        {
            if (!building.IsPaused && !building.IsDestroyed && building.ResourceCanBeGatheredFromHere(itemDefn))
            {
                var distanceToBuilding = location.DistanceTo(building.Location);
                if (distanceToBuilding < dist)
                {
                    closestBuilding = building;
                    dist = distanceToBuilding;
                }
            }
        }
        return closestBuilding?.GetClosestUnreservedGatheringSpotWithItemToReap(location, itemDefn, out dist);
    }

    internal int Chart_GetNumOfItemInTown(string itemId)
    {
        int numInStorage = 0;
        foreach (var building in Buildings)
            numInStorage += building.NumItemsOfTypeInStorage(GameDefns.Instance.ItemDefns[itemId]);

        // see how many are being carried
        var numBeingCarried = 0;
        foreach (var worker in TownWorkerMgr.Workers)
            if (worker.AI.CurrentTask.IsCarryingItem(itemId))
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

    internal NeedData GetHighestNeedForItem(string itemDefnId)
    {
        NeedData highestNeed = null;
        foreach (var building in Buildings)
            foreach (var need in building.Needs)
                if ((need.Type == NeedType.CraftingOrConstructionMaterial || need.Type == NeedType.SellItem || need.Type == NeedType.PersistentBuildingNeed) && need.NeededItem.Id == itemDefnId)
                    if (highestNeed == null || need.Priority > highestNeed.Priority)
                        highestNeed = need;
        return highestNeed;
    }

    // ====================================================================================================
    // Assign/Unassign worker from building
    public void UnassignWorkerFromBuilding(BuildingData data) => GetWorkerInBuilding(data)?.Assignment.AssignTo(Camp);
    public void AssignWorkerToBuilding(BuildingData data) => GetWorkerInBuilding(Camp)?.Assignment.AssignTo(data);

    private WorkerData GetWorkerInBuilding(BuildingData building) => TownWorkerMgr.Workers.FirstOrDefault(worker => worker.Assignment.AssignedTo == building);
    internal int NumTotalItemsInStorage(ItemDefn neededItem) => Buildings.Sum(building => building.NumItemsOfTypeInStorage(neededItem));
    internal int NumTotalStorageSpots() => Buildings.Sum(building => building.NumStorageSpots);

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