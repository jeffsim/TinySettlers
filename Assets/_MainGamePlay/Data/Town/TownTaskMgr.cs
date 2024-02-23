using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TownTaskMgr
{
    // For debugging purposes
    [NonSerialized] public List<PrioritizedTask> LastSeenPrioritizedTasks = new();
    [SerializeReference] TownData Town;
    [SerializeField] PrioritizedTask HighestPriorityTask = new();

    public TownTaskMgr(TownData town)
    {
        Town = town;
    }

    public void AssignTaskToIdleWorker()
    {
        // Find the best task and (if one is found) start it
        var bestTask = GetBestTaskForOneIdleWorker();
        if (bestTask != null)
            bestTask.Worker.AI.StartTask(bestTask);
    }

    // Only do this for one worker per Town update, because need priorities change
    private Task GetBestTaskForOneIdleWorker()
    {
        // Assume we find no task
        HighestPriorityTask.Task = null;

        var idleWorkers = Town.TownWorkerMgr.GetIdleWorkers();
        if (idleWorkers.Count == 0) return null;

        // =====================================================================================
        // FIRST: If an idle worker is standing around holding an Item that they need to transport then assign them to do that
        if (findAndAssignIdleWorkerCarryingItemToBringItemToBuilding(idleWorkers))
            return HighestPriorityTask.Task;

        // =====================================================================================
        // SECOND: If there are no idle worker is standing around holding an Item, then find the building with the highest need that a worker can fulfill

        // Get all needs that have Priority > 0; if none then abort
        var activeNeeds = GetAllActiveNeeds();
        if (activeNeeds.Count == 0) return null;

        // Iterate over all of the needs, creating a list of the highest priority task that can be performed for each Need
        foreach (var need in activeNeeds)
            if (!need.IsBeingFullyMet)
                switch (need.Type)
                {
                    case NeedType.GatherResource: getHigherPriorityTaskIfExists_GatherResource(need, idleWorkers); break;
                    case NeedType.SellItem: getHigherPriorityTaskIfExists_SellItem(need, idleWorkers); break;

                    case NeedType.ClearStorage: getHigherPriorityTaskIfExists_CleanupStorage(need, idleWorkers); break;
                    case NeedType.PickupAbandonedItem: getHigherPriorityTaskIfExists_PickupAbandonedItem(need, idleWorkers); break;
                    case NeedType.CraftingOrConstructionMaterial: getHigherPriorityTaskIfExists_BuildingWantsAnItem(need, idleWorkers); break;
                    case NeedType.CraftGood: getHigherPriorityTaskIfExists_CraftItem(need, idleWorkers); break;
                }

        // Return the highest priority task
        return HighestPriorityTask?.Task;
    }

    bool findAndAssignIdleWorkerCarryingItemToBringItemToBuilding(List<WorkerData> idleWorkers)
    {
        // return true as soon as one worker is assigned
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (!worker.Hands.HasItem) continue; // idle worker isn't carrying anything

            // 'worker' is idle and holding an item.  This is because they were recently sent on a task to pick up an item, and 
            // they picked it up but haven't yet been assigned a new task to bring it anywhere.  In this case, they have already
            // reserved a storage spot for the item, but we'll first look around to see if any building needs the item.  If so, 
            // we'll assign the worker to that building.  If not, we'll deliver the item to the reserved storage spot.
            NeedData highestNeed = GetHighestUnmetNeedForItemInBuildingWithAvailableStorage(worker.Hands.Item.DefnId);
            IItemSpotInBuilding spotToReserve = null;
            if (highestNeed != null)
            {
                // found a building that needs the item and can store it.  Swap out the storage spot reserved for the item with the building's storage spot
                // unreserve the original storage spot
                spotToReserve?.Reservable.Unreserve();

                // Get the nearest storage spot in the building that needs the item and reserve it for this worker to carry the item-in-hand to.
                spotToReserve = highestNeed.BuildingWithNeed.GetClosestEmptyStorageSpot(worker.Location);

                // Tell the Need that we'll be fulfilling it now.
                highestNeed.AssignWorkerToMeetNeed(worker);
            }
            else
            {
                // No building needs it; find a storage spot for it
                if (spotToReserve == null)
                {
                    // We didn't have a storage spot reserved; find the nearest storage spot and reserve it.  If we can't find one, then abandon (drop) the item and abort
                    spotToReserve = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, worker.Location, worker);
                }
                else if (spotToReserve.Building.IsPaused)
                {
                    // We had a storage spot reserved for the item, but it's in a building that is now Paused.  When a building is paused, it's storage spots are no longer valid.
                    // So, we need to find a new storage spot for the item.  If we can't find one, then abandon (drop) the item and abort
                    spotToReserve.Reservable.Unreserve();
                    spotToReserve = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, worker.Location, worker);
                }
                else
                {
                    // The Worker has a storage spot reserved for the item they're holding and it's still valid, so we'll use it. Nothing to do here
                    // Unreserve it since the Task_DeliverItemInHandToStorageSpot task will reserve it.  Note that we're gauranteed to do this task
                    // however if we didn't, then this unreserve would cause issues.  So, warning to future self.
                    spotToReserve.Reservable.Unreserve();
                }

                if (spotToReserve == null)
                {
                    worker.DropItemOnGround();
                    worker.AI.StartIdling();
                    return false;
                }
                highestNeed = worker.OriginalPickupItemNeed;
            }
            HighestPriorityTask.Set(new Task_DeliverItemInHandToStorageSpot(worker, highestNeed, spotToReserve), highestPrioritySoFar);
            return true;
        }

        return false; // no idle worker has an item in hand (that any building needs)
    }

    private void getHigherPriorityTaskIfExists_SellItem(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable
        // Get list of storage spots in nee's building that contain need's item and are unreserved
        var spotsWithItemToSell = need.BuildingWithNeed.GetStorageSpotsWithUnreservedItem(need.NeededItem);
        if (spotsWithItemToSell.Count == 0) return; // if no storage spots have the item then abort

        // =====================================================================================
        // SECOND, determine which idle workers can perform the task.
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Assignable.AssignedTo != need.BuildingWithNeed) continue; // worker must be assigned to the building that sells the item
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!worker.CanSellItems()) continue;

            var closestSpotWithItemToWorker = need.BuildingWithNeed.GetClosestUnreservedStorageSpotWithItemToReapOrSell(worker.Location, out float distanceToGatheringSpot);
            Debug.Assert(closestSpotWithItemToWorker != null, "Should have been caught above");
            float priorityOfMeetingNeedWithThisWorker = need.Priority + getDistanceImpactOnPriority(worker.Location, closestSpotWithItemToWorker.Location);
            if (priorityOfMeetingNeedWithThisWorker > highestPrioritySoFar)
            {
                highestPrioritySoFar = priorityOfMeetingNeedWithThisWorker;
                HighestPriorityTask.Set(new Task_SellItem(worker, need, closestSpotWithItemToWorker), highestPrioritySoFar);
            }
        }
    }

    private void getHigherPriorityTaskIfExists_BuildingWantsAnItem(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable

        // Building that needs the item must have a storage spot available for the item
        if (!need.BuildingWithNeed.HasAvailableStorageSpot) return;

        if (need.BuildingWithNeed.IsPaused || need.BuildingWithNeed.NumWorkers == 0) return;

        // Generate the list of all buildings that the item can be picked up from (ie unreserved and in storage)
        // Note: we allow taking items from buildings that need them IF the building is paused
        var buildingsThatHaveItem = Town.AllBuildings.Where(building => building.HasUnreservedItemOfTypeAndDoesntNeedIt(need.NeededItem)).ToList();
        if (buildingsThatHaveItem.Count == 0) return; // if no buildings have the item then abort

        // =====================================================================================
        // SECOND, determine which idle workers can gather the resource
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Hands.HasItem) continue;                    // if worker is already carrying something then skip
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!worker.CanGoGetItemsBuildingsWant()) continue;

            // Determine which 'building with item' the idle worker can optimally gather from
            StorageSpotData optimalSpot = null;
            foreach (var building in buildingsThatHaveItem)
            {
                if (building == need.BuildingWithNeed) continue; // don't gather from the building that needs the item

                // Optimality of getting item from 'building' is based on distance from building-with-need
                // TODO: In the future, this is where I would add support for user putting thumb on scale re: which buildings to get from
                var closestSpotWithItem = building.GetClosestUnreservedStorageSpotWithItem(need.BuildingWithNeed.Location, need.NeededItem, out float distanceToGatheringSpot);
                if (closestSpotWithItem == null) continue;

                float distanceImpactOnPriority = getDistanceImpactOnPriority(distanceToGatheringSpot);
                float priorityOfMeetingNeedWithBuildingsGatheringSpot = need.Priority + distanceImpactOnPriority;

                // If the priority of using this building's gatheringspot is higher than the priority of the highest priority gatheringspot we've found 
                // so far (and is also higher than the highest priority task we've found so far), then swap
                if (priorityOfMeetingNeedWithBuildingsGatheringSpot > highestPrioritySoFar)
                {
                    optimalSpot = closestSpotWithItem;
                    highestPrioritySoFar = priorityOfMeetingNeedWithBuildingsGatheringSpot;
                }
            }

            // If we have found a gathering spot then we know that the priority of performing this new task is higher than the priority of the
            // highest priority task we've found so far, so replace the highest priority task with this higher-priority gathering task
            if (optimalSpot != null)
            {
                // NOTE that we do not reserve anything at this point, because although we've found the optimal gathering task to perform,
                // a better (non gathering) task to perform may still be found by caller.
                var closestStorageSpot = need.BuildingWithNeed.GetClosestEmptyStorageSpot(optimalSpot.Location);
                Debug.Assert(closestStorageSpot != null, "No storage spot found for item that we're about to gather");
                HighestPriorityTask.Set(new Task_TransportItemFromSpotToSpot(worker, need, optimalSpot, closestStorageSpot), highestPrioritySoFar);
            }
        }
    }

    private void getHigherPriorityTaskIfExists_CraftItem(NeedData need, List<WorkerData> idleWorkers)
    {
        var itemToCraft = need.NeededItem;
        var craftingBuilding = need.BuildingWithNeed;

        // =====================================================================================
        // FIRST, determine if need is meetable
        if (!craftingBuilding.HasUnreservedResourcesInStorageToCraftItem(itemToCraft)) return; // Confirm we have all the resources necessary to craft the item in our storage

        if (craftingBuilding.IsPaused || craftingBuilding.NumWorkers == 0) return;

        var craftingSpot = craftingBuilding.CraftingMgr.GetAvailableCraftingSpot();
        if (craftingSpot == null) return; // No crafting spot available

        // =====================================================================================
        // SECOND, determine which idle workers can perform the task.
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Assignable.AssignedTo != craftingBuilding) continue; // worker must be assigned to the building that crafts the item
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!worker.CanCraftItems()) continue;

            float priorityOfMeetingNeedWithThisWorker = need.Priority + getDistanceImpactOnPriority(worker.Location, craftingSpot.Location);

            if (priorityOfMeetingNeedWithThisWorker > highestPrioritySoFar)
            {
                highestPrioritySoFar = priorityOfMeetingNeedWithThisWorker;
                HighestPriorityTask.Set(new Task_CraftItem(worker, need, craftingSpot), highestPrioritySoFar);
            }
        }
    }

    private void getHigherPriorityTaskIfExists_PickupAbandonedItem(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable
        // Assumed so

        // =====================================================================================
        // SECOND, determine which idle workers can go pick up the abandoned item
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Hands.HasItem) continue;                    // if worker is already carrying something then skip
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!Town.HasAvailablePrimaryOrAssignedStorageSpot(worker)) continue;
            if (!worker.CanPickupAbandonedItems()) continue;

            // availableTasks.Add(new PrioritizedTask(WorkerTask_PickupAbandonedItem.Create(worker, need), need.Priority));
            float priorityOfMeetingNeedWithThisWorker = need.Priority + getDistanceImpactOnPriority(worker.Location, need.AbandonedItemToPickup.Location);

            var closestStorageSpot = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Primary, need.AbandonedItemToPickup.Location);
            Debug.Assert(closestStorageSpot != null, "Should have been caught above");

            if (priorityOfMeetingNeedWithThisWorker > highestPrioritySoFar)
            {
                highestPrioritySoFar = priorityOfMeetingNeedWithThisWorker;
                HighestPriorityTask.Set(new Task_TransportItemFromGroundToSpot(worker, need, closestStorageSpot), highestPrioritySoFar);
            }
        }
    }

    // If here, then a building has a 'cleanup my storage please' need - see if any idleworkers can do it
    private void getHigherPriorityTaskIfExists_CleanupStorage(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable

        // For now, just pick the first item in storage and clean it up; in the future, pick the item closest to the worker, or the item that the building
        // "most wants to get rid of"
        StorageSpotData spotWithItem = need.BuildingWithNeed.GetFirstStorageSpotWithUnreservedItemToRemove();
        if (spotWithItem == null)
            return;

        // =====================================================================================
        // SECOND, determine which idle workers can go get the item to cleanup
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Hands.HasItem) continue;                    // if worker is already carrying something then skip
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!Town.HasAvailablePrimaryOrAssignedStorageSpot(worker)) continue;
            spotWithItem = need.BuildingWithNeed.GetClosestStorageSpotWithUnreservedItemToRemove(worker.Location);

            // If worker can't cleanup items then skip
            // Allow workers that are in a non-primary building that needs cleanup to cleanup items in that building IFF that building is full
            var workerCanCleanUpStorage = worker.CanCleanupStorage();
            var nonPrimaryBuildingWorkerWantsToCleanupOwnBuilding = worker.Assignable.AssignedTo == need.BuildingWithNeed && !worker.Assignable.AssignedTo.HasAvailableStorageSpot;
            if (!workerCanCleanUpStorage && !nonPrimaryBuildingWorkerWantsToCleanupOwnBuilding) continue;

            float distanceToSpotWithItemToCleanup = worker.Location.DistanceTo(need.BuildingWithNeed.Location); // TODO: Ideally would be "storage spot" not "building"
            float distanceImpactOnPriority = getDistanceImpactOnPriority(distanceToSpotWithItemToCleanup);
            float priorityOfMeetingNeedWithThisWorker = need.Priority + distanceImpactOnPriority;

            // Optimality of gathering from 'building' is based on distance from building-with-need
            // TOOD: In the future, this is where I would add support for user putting thumb on scale re: which buildings to gather/not gather from
            var closestStorageSpot = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.Primary, need.BuildingWithNeed.Location);
            Debug.Assert(closestStorageSpot != null, "Should have been caught above");

            if (priorityOfMeetingNeedWithThisWorker > highestPrioritySoFar)
            {
                highestPrioritySoFar = priorityOfMeetingNeedWithThisWorker;
                HighestPriorityTask.Set(new Task_TransportItemFromSpotToSpot(worker, need, spotWithItem, closestStorageSpot), highestPrioritySoFar);
            }
        }
    }

    // need.BuildingWithNeed needs need.NeededItem to be gathered; e.g. woodcutter's hut.
    private void getHigherPriorityTaskIfExists_GatherResource(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable

        // Generate the list of all buildings that the resource can be gathered from (and have an available gatheringspot); if none then abort
        var buildingsThatResourceCanBeGatheredFrom = Town.AllBuildings.Where(building => building.ResourceCanBeGatheredFromHere(need.NeededItem) && !building.IsPaused).ToList();
        if (buildingsThatResourceCanBeGatheredFrom.Count == 0) return;

        // =====================================================================================
        // SECOND, determine which idle workers can gather the resource
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.Hands.HasItem) continue;                    // if worker is already carrying something then skip
            if (worker.Assignable.AssignedTo.IsPaused) continue;
            if (!worker.CanGatherResource(need.NeededItem)) continue;   // If worker can't gather [resource] then skip
            if (!Town.HasAvailablePrimaryOrAssignedStorageSpot(worker)) continue;

            // Determine which 'building with resource' the idle worker can optimally gather from
            GatheringSpotData optimalGatheringSpot = null;
            foreach (var building in buildingsThatResourceCanBeGatheredFrom)
            {
                // Optimality of gathering from 'building' is based on distance from building-with-need
                // TOOD: In the future, this is where I would add support for user putting thumb on scale re: which buildings to gather/not gather from
                var closestGatheringSpot = building.GetClosestUnreservedGatheringSpotWithItemToReap(need.BuildingWithNeed.Location, out float distanceToGatheringSpot);
                if (closestGatheringSpot == null) continue; // if building has no gathering spots then skip

                float distanceImpactOnPriority = getDistanceImpactOnPriority(distanceToGatheringSpot);
                float priorityOfMeetingNeedWithBuildingsGatheringSpot = need.Priority + distanceImpactOnPriority;

                // If the priority of using this building's gatheringspot is higher than the priority of the highest priority gatheringspot we've found 
                // so far (and is also higher than the highest priority task we've found so far), then swap
                if (priorityOfMeetingNeedWithBuildingsGatheringSpot > highestPrioritySoFar)
                {
                    optimalGatheringSpot = closestGatheringSpot;
                    highestPrioritySoFar = priorityOfMeetingNeedWithBuildingsGatheringSpot;
                }
            }

            // If we have found a gathering spot then we know that the priority of performing this new task is higher than the priority of the
            // highest priority task we've found so far, so replace the highest priority task with this higher-priority gathering task
            if (optimalGatheringSpot != null)
            {
                // NOTE that we do not reserve anything at this point, because although we've found the optimal gathering task to perform,
                // a better (non gathering) task to perform may still be found by caller.
                var closestStorageSpot = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, optimalGatheringSpot.Location, worker);
                Debug.Assert(closestStorageSpot != null, "No storage spot found for item that we're about to gather");
                HighestPriorityTask.Set(new Task_GatherResource(worker, need, optimalGatheringSpot, closestStorageSpot), highestPrioritySoFar);
            }
        }
    }

    // float getDistanceImpactOnPriority(Vector3 loc1, Vector3 loc2) => getDistanceImpactOnPriority(Vector3.Distance(loc1, loc2));
    float getDistanceImpactOnPriority(Location loc1, Location loc2) => getDistanceImpactOnPriority(loc1.DistanceTo(loc2));

    private float getDistanceImpactOnPriority(float distance)
    {
        // Distance is largely just used to differentiate between workers, so doesn't need to be large impact
        // clamp distanceToGatheringSpot to 0-100.  At distance 0 (very close) bump priority by 0.01; at distance 100 (very far, bump by 0.0); interpolate
        if (distance > 100) Debug.Log("Oops, 100 isn't enough");
        var maxDistance = 100;
        distance = Mathf.Min(distance, maxDistance);
        float distanceImpactOnPriority = (maxDistance - distance) / maxDistance * 0.01f;
        return distanceImpactOnPriority;
    }

    private List<NeedData> GetAllActiveNeeds()
    {
        List<NeedData> allNeeds = new();
        foreach (var building in Town.AllBuildings)
            foreach (var need in building.Needs)
                if (need.Priority > 0)
                    allNeeds.Add(need);

        allNeeds.AddRange(Town.otherTownNeeds); // Add needs to pick up any items abandoned on the ground

        return allNeeds;
    }

    protected StorageSpotData FindAndReserveOptimalStorageSpotToDeliverItemTo(WorkerData worker)
    {
        var optimalStorageSpotToDeliverItemTo = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, worker.Location, worker);
        if (optimalStorageSpotToDeliverItemTo != null)
            optimalStorageSpotToDeliverItemTo.Reservable.ReserveBy(worker);
        return optimalStorageSpotToDeliverItemTo;
    }

    internal NeedData GetHighestUnmetNeedForItemInBuildingWithAvailableStorage(string itemDefnId)
    {
        NeedData highestNeed = null;
        foreach (var building in Town.AllBuildings)
            if (building.HasAvailableStorageSpot && !building.IsPaused && building.NumWorkers > 0)
                foreach (var need in building.Needs)
                {
                    // only fulfill itemneeds
                    if (need.Type != NeedType.CraftingOrConstructionMaterial && need.Type != NeedType.SellItem && need.Type != NeedType.PersistentBuildingNeed)
                        continue;

                    // don't fulfill item needs if the building that needs the item is paused or has no workers
                    if (need.Type == NeedType.CraftingOrConstructionMaterial && (need.BuildingWithNeed.IsPaused || need.BuildingWithNeed.NumWorkers == 0))
                        continue;

                    // don't fulfill item needs that are already being fulfilled or are for other items
                    if (need.IsBeingFullyMet || need.Priority == 0 || need.NeededItem.Id != itemDefnId)
                        continue;

                    // If here then this is an item need that would like to be fulfilled; is it the highest priority?
                    if (highestNeed == null || need.Priority > highestNeed.Priority)
                        highestNeed = need;
                }
        return highestNeed;
    }
}