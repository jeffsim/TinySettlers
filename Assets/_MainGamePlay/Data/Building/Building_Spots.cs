using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BuildingData : BaseData, ILocation, IOccupiable, IConstructable, IPausable
{
    public List<GatheringSpotData> GatheringSpots = new();
    public List<SleepingSpotData> SleepingSpots = new();

    // Storage related fields
    public List<StorageAreaData> StorageAreas = new();
    public int NumAvailableStorageSpots
    {
        get
        {
            int count = 0;
            foreach (var area in StorageAreas) count += area.NumAvailableSpots;
            return count;
        }
    }
    public int NumStorageSpots
    {
        get
        {
            int count = 0;
            foreach (var area in StorageAreas) count += area.NumStorageSpots;
            return count;
        }
    }
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

    public StorageSpotData GetStorageSpotWithUnreservedItemOfType(ItemDefn itemDefn, ItemClass itemClass = ItemClass.Unset)
    {
        foreach (var spot in StorageSpots)
            if (!spot.Reservable.IsReserved && spot.Container.FirstItem != null && spot.Container.FirstItem.DefnId == itemDefn.Id)// && spot.ItemInStorage.Defn.ItemClass == itemClass)
                return spot;

        return null;
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
    public int NumItemsOfTypeInStorage(ItemDefn itemDefn)
    {
        int num = 0;
        foreach (var area in StorageAreas)
            num += area.NumItemsOfTypeInStorage(itemDefn);
        return num;
    }

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
                var itemNeed = getItemNeedForItem(spot.Container.FirstItem.Defn);
                if (!allowRemovingNeededItems && itemNeed != null)
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
}
