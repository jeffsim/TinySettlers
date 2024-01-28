using System;
using UnityEngine;

public enum WorkerTask_CraftItemSubstate
{
    GotoSpotWithResource = 0,
    PickupResource = 1,
    CarryResourceToCraftingSpot = 2,
    DropResourceInCraftingSpot = 3,
    ProduceGood = 4,
};

[Serializable]
public class WorkerTask_CraftItem : WorkerTask
{
    public override string ToString() => "Craft item";
    internal override string getDebuggerString() => $"Craft Item {CraftingItemDefnId}";

    public override TaskType Type => TaskType.CraftGood;

    // The id of the item being crafted
    [SerializeField] string CraftingItemDefnId;

    // Lookup.  TODO: Cache.
    ItemDefn itemBeingCrafted => GameDefns.Instance.ItemDefns[CraftingItemDefnId];

    [SerializeField] CraftingSpotData reservedCraftingSpot;
    [SerializeField] StorageSpotData reservedStorageSpot;

#if UNITY_INCLUDE_TESTS
    public CraftingSpotData CraftingSpot => reservedCraftingSpot;
    public StorageSpotData ReservedStorageSpot => reservedStorageSpot;
#endif

    public const float secondsToPickupSourceResource = 0.5f;
    public const float secondsToCraft = 1;
    public const float secondsToPickupCraftedGood = 0.5f;

    public override bool IsWalkingToTarget => substate == (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource || substate == (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot;

    public override ItemDefn GetTaskItem() => CraftingItemDefnId == null ? null : GameDefns.Instance.ItemDefns[CraftingItemDefnId];

    public override string ToDebugString()
    {
        var str = "Craft item\n";
        // str += "  Gather from: " + optimalGatheringSpot + " (" + optimalGatheringSpot.Building + "), gatherspot: " + optimalGatheringSpot.InstanceId + "\n";
        str += "  substate: " + substate;
        // switch (substate)
        // {
        //     case (int)WorkerTask_CraftItemSubstate.GotoGatheringSpot: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, optimalGatheringSpot.WorldLoc).ToString("0.0"); break;
        //     case (int)WorkerTask_CraftItemSubstate.ReapGatherableResource: str += "; per = " + getPercentSubstateDone(secondsToReap); break;
        //     case (int)WorkerTask_CraftItemSubstate.PickupGatherableResource: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
        //     default: Debug.LogError("unknown substate " + substate); break;
        // }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_CraftItem Create(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot, StorageSpotData storageSpotToStoreCraftedItemIn)
    {
        return new WorkerTask_CraftItem(worker, needData, craftingSpot, storageSpotToStoreCraftedItemIn);
    }

    private WorkerTask_CraftItem(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot, StorageSpotData storageSpotToStoreCraftedItemIn) : base(worker, needData)
    {
        CraftingItemDefnId = needData.NeededItem.Id;
        this.reservedCraftingSpot = craftingSpot;
        reservedStorageSpot = storageSpotToStoreCraftedItemIn;
    }

    public override void Start()
    {
        base.Start();

        // Now that we've actually started the task, we can reserve the already-determined-to-be optimal gathering spot that was passed in above.
        // reserveCraftingSpot(craftingSpot);
        // reserveStorageSpot(reservedStorageSpot);


        // Reserve our storage spots with resources that we will consume to craft the good
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
                reserveCraftingResourceStorageSpotForItem(resource.Item);

        // Reserve a spot to do the crafting
        reservedCraftingSpot = reserveBuildingCraftingSpot(Worker.AssignedBuilding);

        // If the crafted item can be stored (vs e.g. gold), then reserve the spot to store it
        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
            reservedStorageSpot = reserveStorageSpotClosestToWorldLoc(reservedCraftingSpot.WorldLoc);

        // Start out walking to the storage spot with the first resource wek'll use for crafting
        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
    }

    public override void OnBuildingDestroyed(BuildingData destroyedBuilding)
    {
        Debug.Assert(false, "nyi");
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        Debug.Assert(false, "nyi");
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource: // go to resource spot
                if (MoveTowards(nextCraftingResourceStorageSpotToGetFrom.WorldLoc, distanceMovedPerSecond))
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.PickupResource);
                break;

            case (int)WorkerTask_CraftItemSubstate.PickupResource:
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    // Remove the item from the spot (and the game, technically), and unreserve the spot so that it can be used by other Workers
                    unreserveBuildingCraftingResourceSpot(nextCraftingResourceStorageSpotToGetFrom);
                    nextCraftingResourceStorageSpotToGetFrom.RemoveItem();
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                if (MoveTowards(reservedCraftingSpot.WorldLoc, distanceMovedPerSecond))
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot);
                break;

            case (int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // do something?
                    if (HasMoreCraftingResourcesToGet())
                    {
                        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
                        gotoSubstate((int)WorkerTask_CraftItemSubstate.GotoSpotWithResource);
                    }
                    else
                        gotoSubstate((int)WorkerTask_CraftItemSubstate.ProduceGood);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.ProduceGood: // craft
                if (getPercentSubstateDone(secondsToCraft) == 1)
                {
                    // Done crafting; let someone else craft in it and goto next substate
                    unreserveBuildingCraftingSpot(reservedCraftingSpot);
                    if (itemBeingCrafted.GoodType == GoodType.explicitGood)
                    {
                        // We've already reserved a storage spot for the crafted item, but other stored items may have changed since we reserved the spot.
                        reservedStorageSpot = getBetterStorageSpotThanSpotIfExists(reservedStorageSpot);
                        gotoSubstate((int)WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot);
                    }
                    else
                    {
                        // implicit good (e.g. gold) - done
                        Worker.AssignedBuilding.Town.Gold += 100; // todo: hardcoded
                        CompleteTask();
                    }
                }
                break;


            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}