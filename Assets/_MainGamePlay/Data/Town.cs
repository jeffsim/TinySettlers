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
    [NonSerialized] public Action<WorkerData> OnWorkerCreated;

    public TownTaskMgr TownTaskMgr;

    // Current Map
    public BuildingData Camp;
    public List<TileData> Tiles = new();
    public List<WorkerData> Workers = new();
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
    }

    public void InitializeOnFirstEnter()
    {
        lastGameTime = 0;
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
                    building.GetEmptyStorageSpot().ItemContainer.SetItem(new ItemData() { DefnId = item.Item.Id });
        }
        TownTaskMgr = new(this);
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

        OnBuildingAdded?.Invoke(building);

        return building;
    }

    public void DestroyBuilding(BuildingData building)
    {
        Tiles[building.TileY * Defn.Width + building.TileX].BuildingInTile = null;

        Buildings.Remove(building);
        building.Destroy();
    }

    public void DestroyWorker(WorkerData worker)
    {
        Workers.Remove(worker);
        worker.OnDestroyed();
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

        foreach (var building in Buildings)
            building.Update();

        // e.g. pick up items on the ground
        updateTownNeeds();

        TownTaskMgr.AssignTaskToIdleWorker();

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
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused)
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
            if (building.Defn.CanStoreItems && building.HasAvailableStorageSpot && !building.IsPaused)
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

    internal int Chart_GetNumOfItemInTown(string itemId)
    {
        int numInStorage = 0;
        foreach (var building in Buildings)
            numInStorage += building.NumItemsOfTypeInStorage(GameDefns.Instance.ItemDefns[itemId]);

        // see how many are being carried
        var numBeingCarried = 0;
        foreach (var worker in Workers)
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
    internal void UnassignWorkerFromBuilding(BuildingData data) => GetWorkerInBuilding(data)?.Assignment.AssignTo(Camp);
    internal void AssignWorkerToBuilding(BuildingData data) => GetWorkerInBuilding(Camp)?.Assignment.AssignTo(data);

    private WorkerData GetWorkerInBuilding(BuildingData building) => Workers.FirstOrDefault(worker => worker.Assignment.AssignedTo == building);
    internal int NumBuildingWorkers(BuildingData building) => Workers.Count(worker => worker.Assignment.AssignedTo == building);
    internal int NumTotalItemsInStorage(ItemDefn neededItem) => Buildings.Sum(building => building.NumItemsOfTypeInStorage(neededItem));
    internal int NumTotalStorageSpots() => Buildings.Sum(building => building.NumStorageSpots);

    // called when a building is requesting an available worker be assigned to it
    // For now, assignment is done from Camp, so just check if Camp has any workers
    internal bool WorkerIsAvailable() => NumBuildingWorkers(Camp) > 0;

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