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
        GetBestTaskForOneIdleWorker();
        if (HighestPriorityTask.Task != null)
        {
            var task = HighestPriorityTask.Task;
            task.Worker.CurrentTask = task;
            task.Worker.CurrentTask.Start();
        }
    }

    // Only do this for one worker per Town update, because need priorities change
    private PrioritizedTask GetBestTaskForOneIdleWorker()
    {
        // Assume we find no task
        HighestPriorityTask.Task = null;

        List<WorkerData> idleWorkers = new(Town.Workers.FindAll(w => w.IsIdle)); // todo (perf): keep list of idle
        if (idleWorkers.Count == 0) return null;

        // =====================================================================================
        // FIRST: If an idle worker is standing around holding an Item that they need to transport then assign them to do that
        if (findAndAssignIdleWorkerCarryingItemToBringItemToBuilding(idleWorkers))
            return null;

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
                    case NeedType.ClearStorage: getHigherPriorityTaskIfExists_CleanupStorage(need, idleWorkers); break;
                }

        // Return the highest priority task
        return HighestPriorityTask;
    }


    // If here, then a building has a 'cleanup my storage please' need - see if any idleworkers can do it
    private void getHigherPriorityTaskIfExists_CleanupStorage(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable

        // Confirm that there is at least one storage spot that can store the cleaned-up item; if none then abort
        var tempStorageSpot = Town.GetAvailableStorageSpot(StorageSpotSearchType.Primary);
        if (tempStorageSpot == null)
            return;

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
            if (worker.ItemInHand != null) continue;                    // if worker is already carrying something then skip

            // If worker can't cleanup items then skip
            // Allow workers that are in a non-primary building that needs cleanup to cleanup items in that building IFF that building is full
            var workerCanCleanUpStorage = worker.CanCleanupStorage();
            var nonPrimaryBuildingWorkerWantsToCleanupOwnBuilding = worker.AssignedBuilding == need.BuildingWithNeed && !worker.AssignedBuilding.HasAvailableStorageSpot;
            if (!workerCanCleanUpStorage && !nonPrimaryBuildingWorkerWantsToCleanupOwnBuilding) continue;

            float distanceToSpotWithItemToCleanup = Vector3.Distance(worker.WorldLoc, need.BuildingWithNeed.WorldLoc); // TODO: Ideally would be "storage spot" not "building"
            float distanceImpactOnPriority = getDistanceImpactOnPriority(distanceToSpotWithItemToCleanup);
            float priorityOfMeetingNeedWithThisWorker = need.Priority + distanceImpactOnPriority;

            if (priorityOfMeetingNeedWithThisWorker > highestPrioritySoFar)
            {
                highestPrioritySoFar = priorityOfMeetingNeedWithThisWorker;
                HighestPriorityTask.Set(WorkerTask_PickupItemFromBuilding.Create(worker, need, spotWithItem, tempStorageSpot), highestPrioritySoFar);
            }
        }
    }

    // need.BuildingWithNeed needs need.NeededItem to be gathered; e.g. woodcutter's hut.
    private void getHigherPriorityTaskIfExists_GatherResource(NeedData need, List<WorkerData> idleWorkers)
    {
        // =====================================================================================
        // FIRST, determine if need is meetable

        // Confirm that there is at least one storage spot that can store the gathered resource; if none then abort
        if (!Town.HasAvailableStorageSpot())
            return;

        // Generate the list of all buildings that the resource can be gathered from (and have an available gatheringspot); if none then abort
        var buildingsThatResourceCanBeGatheredFrom = Town.Buildings.Where(building => building.ResourceCanBeGatheredFromHere(need.NeededItem)).ToList();
        if (buildingsThatResourceCanBeGatheredFrom.Count == 0) return;

        // =====================================================================================
        // SECOND, determine which idle workers can gather the resource
        float highestPrioritySoFar = HighestPriorityTask.Task == null ? 0 : HighestPriorityTask.Priority;
        foreach (var worker in idleWorkers)
        {
            if (worker.ItemInHand != null) continue;                    // if worker is already carrying something then skip
            if (!worker.CanGatherResource(need.NeededItem)) continue;   // If worker can't gather [resource] then skip

            // Determine which 'building with resource' the idle worker can optimally gather from
            GatheringSpotData optimalGatheringSpot = null;
            foreach (var building in buildingsThatResourceCanBeGatheredFrom)
            {
                // Optimality of gathering from 'building' is based on distance from building-with-need
                // TOOD: In the future, this is where I would add support for user putting thumb on scale re: which buildings to gather/not gather from
                var closestGatheringSpot = building.GetClosestUnreservedGatheringSpotWithItemToReap(need.BuildingWithNeed.WorldLoc, out float distanceToGatheringSpot);
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
                var closestStorageSpot = Town.GetClosestAvailableStorageSpot(StorageSpotSearchType.AssignedBuildingOrPrimary, optimalGatheringSpot.WorldLoc, out float _, worker);
                Debug.Assert(closestStorageSpot != null, "No storage spot found for item that we're about to gather");
                HighestPriorityTask.Set(WorkerTask_PickupGatherableResource.Create(worker, need, optimalGatheringSpot, closestStorageSpot), highestPrioritySoFar);
            }
        }
    }

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
        foreach (var building in Town.Buildings)
            foreach (var need in building.Needs)
                if (need.Priority > 0)
                    allNeeds.Add(need);

        // allNeeds.AddRange(otherTownNeeds); // Add needs to pick up any items abandoned on the ground

        return allNeeds;
    }

    private bool findAndAssignIdleWorkerCarryingItemToBringItemToBuilding(List<WorkerData> idleWorkers)
    {
        // return true as soon as one worker is assigned
        foreach (var worker in idleWorkers)
        {
            if (worker.ItemInHand == null) continue; // idle worker isn't carrying anything
            Debug.Assert(worker.StorageSpotReservedForItemInHand != null, $"Worker {worker} is carrying item {worker.ItemInHand} but doesn't have a storage spot reserved for it");

            // 'worker' is idle and holding an item.  This is because they were recently sent on a task to pick up an item, and 
            // they picked it up but haven't yet been assigned a new task to bring it anywhere.  In this case, they have already
            // reserved a storage spot for the item, but we'll first look around to see if any building needs the item.  If so, 
            // we'll assign the worker to that building.  If not, we'll deliver the item to the reserved storage spot.
            NeedData highestNeed = GetHighestUnmetNeedForItemInBuildingWithAvailableStorage(worker.ItemInHand.DefnId);
            if (highestNeed != null)
            {
                // found a building that needs the item and can store it.  Swap out the storage spot reserved for the item with the building's storage spot
                // unreserve the original storage spot
                worker.StorageSpotReservedForItemInHand.Unreserve();

                // Get the nearest storage spot in the building that needs the item and reserve it for this worker to carry the item-in-hand to.
                worker.StorageSpotReservedForItemInHand = highestNeed.BuildingWithNeed.GetClosestEmptyStorageSpot(worker.WorldLoc);
                worker.StorageSpotReservedForItemInHand.ReserveBy(worker);

                // Tell the Need that we'll be fulfilling it now.
                highestNeed.AssignWorkerToMeetNeed(worker);
            }
            else
            {
                // No building needs it; continue on and deliver this item to whatever originally requested it.  Note again that we've already reserved storage for it
                highestNeed = worker.OriginalPickupItemNeed;
            }
            WorkerTask_DeliverItemInHandToStorageSpot.CreateAndStart(worker, highestNeed);
            return true;
        }

        return false; // no idle worker has an item in hand (that any building needs)
    }

    internal NeedData GetHighestUnmetNeedForItemInBuildingWithAvailableStorage(string itemDefnId)
    {
        NeedData highestNeed = null;
        foreach (var building in Town.Buildings)
            if (building.HasAvailableStorageSpot)
                foreach (var need in building.Needs)
                    if (!need.IsBeingFullyMet &&
                        (need.Type == NeedType.CraftingOrConstructionMaterial || need.Type == NeedType.SellGood || need.Type == NeedType.PersistentBuildingNeed) &&
                        need.NeededItem.Id == itemDefnId)
                        if (highestNeed == null || need.Priority > highestNeed.Priority)
                            highestNeed = need;
        return highestNeed;
    }
}