using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingTaskMgrData
{
    [SerializeField] BuildingData Building;

    TownData Town => Building.Town;
    BuildingDefn Defn => Building.Defn;

    public BuildingTaskMgrData(BuildingData building)
    {
        Building = building;
    }

    internal void GetAvailableTasksForWorker(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        addTasks_GatherResources(availableTasks, worker);
        addTasks_CraftItems(availableTasks, worker);
        addTasks_CourierItems(availableTasks, worker, allTownNeeds);
        addTasks_CleanupBuildingStorage(availableTasks, worker, allTownNeeds);
        addTasks_CleanupAbandonedItems(availableTasks, worker, allTownNeeds);
        addTasks_sellGoodsThatAreInTheBuilding(availableTasks, worker, allTownNeeds);
        addTasks_RequestGoodsThatBuildingCanSell(availableTasks, worker, allTownNeeds);
    }

    private void addTasks_RequestGoodsThatBuildingCanSell(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Building.Defn.CanSellGoods) return;

        foreach (var need in allTownNeeds)
        {
            if (need.Type != NeedType.SellGood) continue;

            // TODO
        }
    }

    private void addTasks_sellGoodsThatAreInTheBuilding(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.CanSellGoods) return;

        foreach (var need in allTownNeeds)
        {
            if (need.Type != NeedType.SellGood) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // Do we have the item in storage to sell?
            StorageSpotData spotWithItemToSell = Building.GetStorageSpotWithUnreservedItemOfType(need.NeededItem);
            if (spotWithItemToSell == null) continue;

            // Found a storage spot in this building with an item to sell
            availableTasks.Add(new PrioritizedTask(WorkerTask_SellGood.Create(worker, need, spotWithItemToSell), need.Priority));
        }
    }

    private void addTasks_CleanupAbandonedItems(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
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

            // Ensure a storage location exists; we don't care if it's closest as long as it's valid.  The determination of closest is 
            // deferred until the task is started since (a) it's more costly to calculate and (b) we may not opt to do this task
            if (!Town.HasAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, worker)) continue;

            // Found a storage spot to hold the item
            availableTasks.Add(new PrioritizedTask(WorkerTask_PickupAbandonedItem.Create(worker, need), need.Priority));
        }
    }

    private void addTasks_CleanupBuildingStorage(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.WorkersCanFerryItems) return;

        // Ensure a storage location exists; we don't care if it's closest as long as it's valid.  The determination of closest is 
        // deferred until the task is started since (a) it's more costly to calculate and (b) we may not opt to do this task
        if (!Town.HasAvailableStorageSpot()) return;

        foreach (var need in allTownNeeds)
        {
            // Only looking for cleanup needs
            if (need.Type != NeedType.ClearStorage) continue;

            // If no priority then don't try to meet it
            if (need.Priority == 0) continue;

            // don't meet the needs of destroyed buildings
            if (need.BuildingWithNeed.IsDestroyed) continue;

            // Minion must have a path to the building
            if (!worker.HasPathToBuilding(need.BuildingWithNeed)) continue;

            // need.BuildingWithNeed is saying "I have a need to have items removed from my storage"
            // The worker needs to find the closest item in that building that can/should be removed, and the closest PrimaryStorage spot that is empty and avaialble
            // If those are found, then create a ferryitem task to move it to storage
            StorageSpotData spotWithItemToMove = GetItemToRemoveFromStorage(worker, need.BuildingWithNeed);
            if (spotWithItemToMove == null) continue;

            // Hm; Ferry Item takes a destination building, forcing me to find the closest storage spot in that building.  I don't want to do that since it's redone in the task.start
            // and doing it here is wasted work if this Task isn't chosen.
            StorageSpotData destinationStorageSpot = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Primary, spotWithItemToMove.WorldLoc);
            if (destinationStorageSpot == null) continue;

            // Found a resource that can meet the need - calculate how well this minion can meet the need (score)
            availableTasks.Add(new PrioritizedTask(WorkerTask_FerryItem.Create(worker, spotWithItemToMove, null), need.Priority));
        }
    }

    private void addTasks_CourierItems(List<PrioritizedTask> availableTasks, WorkerData worker, List<NeedData> allTownNeeds)
    {
        if (!Defn.WorkersCanFerryItems) return;

        // Ensure a storage location exists; we don't care if it's closest as long as it's valid.  The determination of closest is 
        // deferred until the task is started since (a) it's more costly to calculate and (b) we may not opt to do this task
        if (!Town.HasAvailableStorageSpot()) return;

        // for now, find the first instance where a building needs a resource and the resource is in another building.
        foreach (var need in allTownNeeds)
        {
            // Only looking for crafting, construction, and selling needs
            if (need.Type != NeedType.CraftingOrConstructionMaterial) continue;

            // stockers only meet item needs
            if (need.NeedCoreType != NeedCoreType.Item) continue;

            // If no priority then don't try to meet it.  If another worker is meeting the need then this will be set to 0 in UpdateNeedPriorities
            if (need.Priority == 0) continue;

            // don't meet the needs of destroyed buildings
            if (need.BuildingWithNeed.IsDestroyed) continue;

            // Worker must have a path to the building
            if (!worker.HasPathToBuilding(need.BuildingWithNeed)) continue;

            // Building with Need must have a storage spot to hold the need
            if (!need.BuildingWithNeed.HasAvailableStorageSpot) continue;

            // Find closest accessible resource to this worker that can meet the need.  This function ensures that there is a path from the building to the resource.
            var closestStorageSpotWithItem = Town.GetClosestItemOfType(need.NeededItem, need.ItemClass, need.BuildingWithNeed);
            if (closestStorageSpotWithItem == null) continue;

            // TODO: Calculate how well this worker can meet the need; e.g. a closer worker can meet the need better than this one.

            // Good to go - add the task as a possible choice
            availableTasks.Add(new PrioritizedTask(WorkerTask_FerryItem.Create(worker, closestStorageSpotWithItem, need.BuildingWithNeed), need.Priority));
        }
    }

    private void addTasks_GatherResources(List<PrioritizedTask> availableTasks, WorkerData worker)
    {
        if (!Defn.CanGatherResources) return;
        if (Building.IsStorageFull) return;

        // Ensure a storage location exists; we don't care if it's closest as long as it's valid.  The determination of closest is 
        // deferred until the task is started since (a) it's more costly to calculate and (b) we may not opt to do this task
        if (!Town.HasAvailableStorageSpot()) return;

        foreach (var need in Building.GatheringNeeds)
        {
            if (need.Priority == 0) continue; // no active need for the resource (e.g. already being fully met)

            BuildingData buildingToGatherFrom = Town.getNearestResourceSource(worker, need.NeededItem);
            if (buildingToGatherFrom == null) continue; // no building to gather from

            // Good to go - add the task as a possible choice
            availableTasks.Add(new PrioritizedTask(WorkerTask_GatherResource.Create(worker, need.NeededItem, buildingToGatherFrom), need.Priority));
        }
    }

    private void addTasks_CraftItems(List<PrioritizedTask> availableTasks, WorkerData worker)
    {
        if (!Defn.CanCraft) return;
        if (Building.IsStorageFull) return;
        if (Building.NumAvailableCraftingSpots == 0) return;

        foreach (var itemToCraft in Defn.CraftableItems)
        {
            var priority = getPriorityOfCraftingItem(itemToCraft);
            if (priority == 0) continue;

            // Do we have the items in storage to craft the item?
            // TODO: Option to spin off a "go get item from storage" task if couriers aren't doing it?
            if (!hasUnreservedResourcesInStorageToCraftItem(itemToCraft)) continue;

            // Do we have storage for the crafted item?
            // TODO: Option to move item from our storage to a storage building if couriers aren't doing it?
            bool isImplicitGood = itemToCraft.GoodType == GoodType.implicitGood;
            if (!isImplicitGood && !Building.HasAvailableStorageSpot) continue;

            // Good to go - add the task as a possible choice
            availableTasks.Add(new PrioritizedTask(WorkerTask_CraftItem.Create(worker, itemToCraft), priority));
        }
    }

    // ===================================================

    private StorageSpotData GetItemToRemoveFromStorage(WorkerData worker, BuildingData building)
    {
        float distToClosest = float.MaxValue;
        StorageSpotData closestSpotWithItemToRemove = null;

        // Called when a cleaner is looking for something to remove from our storage.
        foreach (var area in building.StorageAreas)
            foreach (var spot in area.StorageSpots)
                if (!spot.IsEmpty && !spot.IsReserved)
                {
                    if (itemIsNeededForCrafting(spot.ItemInSpot, building))
                        continue;
                    var dist = Vector3.Distance(worker.WorldLoc, spot.WorldLoc); // TODO (PERF): There are a lot of Vector3.Distance calls happening...
                    if (dist < distToClosest)
                    {
                        distToClosest = dist;
                        closestSpotWithItemToRemove = spot;
                    }
                }

        return closestSpotWithItemToRemove;
    }

    private bool itemIsNeededForCrafting(ItemData item, BuildingData building)
    {
        foreach (var need in building.CraftingResourceNeeds)
            if (need.NeededItem.Id == item.DefnId)
                return true;
        return false;
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
        foreach (var area in Building.StorageAreas)
            num += area.NumUnreservedItemsInStorage(itemDefn);
        return num >= count;
    }

}
