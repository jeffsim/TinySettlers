using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void OnItemAddedToGroundEvent(ItemData item);

public enum StorageSpotSearchType { Any, Primary, AssignedBuildingOrPrimary }

[Serializable]
public class TileStack
{
    public List<BuildingData> Buildings = new();

    public Vector2Int HexTile;

    internal void AddBuilding(BuildingData building)
    {
        Buildings.Add(building);
        // Debug.Log($"Added {building} to TileStack {HexTile}. Buildings now in stack: {Buildings.Count}");

        // All buildings below this have changed position in the stack. For now just change all
        foreach (var buildingInStack in Buildings)
            buildingInStack.OnPositionInTileStackChanged?.Invoke();
    }

    public void RemoveBuilding(BuildingData building)
    {
        Buildings.Remove(building);
        // Debug.Log($"Removed {building} from TileStack {HexTile}. Buildings left: {Buildings.Count}");

        // All buildings above this have changed position in the stack. For now just change all
        foreach (var buildingInStack in Buildings)
            buildingInStack.OnPositionInTileStackChanged?.Invoke();
    }

    internal bool IsTopBuilding(BuildingData data)
    {
        return Buildings.Count > 0 && Buildings[Buildings.Count - 1] == data;
    }
}

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
    [NonSerialized] public Action<BuildingData> OnBuildingMoved;

    public TownTaskMgr TownTaskMgr;
    public TownWorkerMgr TownWorkerMgr;
    public TownTimeMgr TimeMgr;

    public int NumHomedWorkers => AllBuildings.Sum(building => building.Occupiable == null ? 0 : building.Occupiable.NumOccupants);

    // Current Map
    public BuildingData Camp;
    public List<TileStack> TileStacks;
    public List<BuildingData> AllBuildings = new();
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
    }

    public void InitializeOnFirstEnter()
    {
        lastGameTime = GameTime.time = 0;

        TileStacks = new();
        for (int y = 0; y < Defn.Height; y++)
            for (int x = 0; x < Defn.Width; x++)
                TileStacks.Add(new TileStack() { HexTile = new Vector2Int(x, y) });

        Gold = 0;
        TownWorkerMgr = new(this);
        TimeMgr = new();

        AllBuildings.Clear();
        otherTownNeeds.Clear();
        foreach (var tbDefn in Defn.Buildings)
        {
            if (!tbDefn.IsEnabled) continue;

            BuildingData building;
            building = ConstructBuilding(tbDefn.Building, tbDefn.TileX, tbDefn.TileY);
            if (building.Defn.BuildingClass == BuildingClass.Camp)
                Camp = building;
#if UNITY_INCLUDE_TESTS
            building.TestId = tbDefn.TestId;
#endif
            for (int i = 0; i < tbDefn.NumWorkersStartAtBuilding; i++)
                CreateWorkerInBuilding(building);

            foreach (var item in tbDefn.StartingItemsInBuilding)
                for (int i = 0; i < item.Count; i++)
                    building.GetEmptyStorageSpot().Container.AddItem(new ItemData() { DefnId = item.Item.Id });
        }
        TownTaskMgr = new(this);
    }

    public WorkerData CreateWorkerInBuilding(BuildingData building)
    {
        var worker = new WorkerData(GameDefns.Instance.WorkerDefns["camper"], building);
        TownWorkerMgr.AddWorker(worker);
        FindHomesForUnhomedWorkers();
        return worker;
    }

    public BuildingData ConstructBuilding(BuildingDefn buildingDefn, int tileX, int tileY)
    {
        var building = new BuildingData(buildingDefn, tileX, tileY);
        GetTileStackForHexTile(tileX, tileY).AddBuilding(building);

        building.Initialize(this);
        AllBuildings.Add(building);
        OnBuildingAdded?.Invoke(building);
        FindHomesForUnhomedWorkers();
        return building;
    }

    public TileStack GetTileStackForHexTile(Vector2Int hexTile) => TileStacks[hexTile.y * Defn.Width + hexTile.x];
    public TileStack GetTileStackForHexTile(int tileX, int tileY) => TileStacks[tileY * Defn.Width + tileX];

    private void FindHomesForUnhomedWorkers()
    {
        foreach (var worker in TownWorkerMgr.Workers)
            if (!worker.Occupier.HasHome)
            {
                var availableHome = AllBuildings.FirstOrDefault(building => building.Defn.Occupiable.WorkersCanLiveHere && building.Occupiable.HasRoom);
                if (availableHome == null)
                    return; // no more homes with available space

                availableHome.Occupiable.AddOccupant(worker);
            }
    }

    public void DestroyBuilding(BuildingData building)
    {
        GetTileStackForHexTile(building.TileX, building.TileY).RemoveBuilding(building);
        AllBuildings.Remove(building);
        building.Destroy();
        OnBuildingRemoved?.Invoke(building);
        FindHomesForUnhomedWorkers();
    }

    public void MoveBuilding(BuildingData building, Vector2Int hexTile)
    {
        if (hexTile.x == building.TileX && hexTile.y == building.TileY)
            return;
        GetTileStackForHexTile(new Vector2Int(building.TileX, building.TileY)).RemoveBuilding(building);
        building.MoveTo(hexTile);
        GetTileStackForHexTile(hexTile).AddBuilding(building);
        OnBuildingMoved?.Invoke(building);
    }

    public void Update()
    {
        GameTime.Update();
        TimeMgr.Update();

        foreach (var building in AllBuildings)
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

    internal BuildingData GetNearestResourceSource(Location loc, ItemDefn itemDefn)
    {
        return loc.GetClosest(AllBuildings, building => building.ResourceCanBeGatheredFromHere(itemDefn));
    }

    internal void AddItemToGround(ItemData item, Location loc)
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

        foreach (var building in AllBuildings)
        {
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused)
            {
                var buildingMatchesSearchType = searchType switch
                {
                    StorageSpotSearchType.Any => true,
                    StorageSpotSearchType.Primary => building.Defn.IsPrimaryStorage,
                    StorageSpotSearchType.AssignedBuildingOrPrimary => building.Defn.IsPrimaryStorage || building == worker.Assignable.AssignedTo,
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

    public StorageSpotData GetClosestAvailableStorageSpot(StorageSpotSearchType searchType, Location location, WorkerData worker = null) => GetClosestAvailableStorageSpot(searchType, location, worker, out float _);
    public StorageSpotData GetClosestAvailableStorageSpot(StorageSpotSearchType searchType, Location location, WorkerData worker, out float dist)
    {
        if (searchType == StorageSpotSearchType.AssignedBuildingOrPrimary)
            Debug.Assert(worker != null, "worker must be specified for AnyAssignedBuildingOrPrimary");

        BuildingData closestBuilding = null;
        dist = float.MaxValue;
        foreach (var building in AllBuildings)
        {
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused)
            {
                var buildingMatchesSearchType = searchType switch
                {
                    StorageSpotSearchType.Any => true,
                    StorageSpotSearchType.Primary => building.Defn.IsPrimaryStorage,
                    StorageSpotSearchType.AssignedBuildingOrPrimary => building.Defn.IsPrimaryStorage || building == worker.Assignable.AssignedTo,
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

    public GatheringSpotData GetClosestAvailableGatheringSpot(Location location, ItemDefn itemDefn, WorkerData worker = null) => GetClosestAvailableGatheringSpot(location, itemDefn, worker, out float _);
    public GatheringSpotData GetClosestAvailableGatheringSpot(Location location, ItemDefn itemDefn, WorkerData worker, out float dist)
    {
        BuildingData closestBuilding = null;
        dist = float.MaxValue;
        foreach (var building in AllBuildings)
        {
            if (!building.IsPaused && building.ResourceCanBeGatheredFromHere(itemDefn))
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
        foreach (var building in AllBuildings)
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
        foreach (var building in AllBuildings)
            foreach (var need in building.Needs)
                if (need.Type == NeedType.CraftingOrConstructionMaterial && need.NeededItem.Id == itemId)
                    total += need.Priority;
        return total;
    }

    internal NeedData GetHighestNeedForItem(string itemDefnId)
    {
        NeedData highestNeed = null;
        foreach (var building in AllBuildings)
            foreach (var need in building.Needs)
                if ((need.Type == NeedType.CraftingOrConstructionMaterial || need.Type == NeedType.SellItem || need.Type == NeedType.PersistentBuildingNeed) && need.NeededItem.Id == itemDefnId)
                    if (highestNeed == null || need.Priority > highestNeed.Priority)
                        highestNeed = need;
        return highestNeed;
    }

    // ====================================================================================================
    // Assign/Unassign worker from building
    public void UnassignWorkerFromBuilding(BuildingData data) => GetWorkerInBuilding(data)?.Assignable.AssignTo(Camp);
    public void AssignWorkerToBuilding(BuildingData data) => GetWorkerInBuilding(Camp)?.Assignable.AssignTo(data);

    private WorkerData GetWorkerInBuilding(BuildingData building) => TownWorkerMgr.Workers.FirstOrDefault(worker => worker.Assignable.AssignedTo == building);
    internal int NumTotalItemsInStorage(ItemDefn neededItem)
    {
        int numInStorage = 0;
        foreach (var building in AllBuildings)
            numInStorage += building.NumItemsOfTypeInStorage(neededItem);
        return numInStorage;
    }
    internal int NumTotalStorageSpots()
    {
        int count = 0;
        foreach (var building in AllBuildings)
            count += building.NumStorageSpots;
        return count;
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

    internal int NumBuildingsInHexTileNotCountingBuilding(BuildingData data, Vector2Int hexTile)
    {
        var tileStack = GetTileStackForHexTile(hexTile.x, hexTile.y);
        int count = 0;
        foreach (var building in tileStack.Buildings)
            if (building != data)
                count++;
        return count;
    }
}