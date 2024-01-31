using System;
using UnityEngine;

public enum WorkerTask_CraftItemSubstate
{
    GotoSpotWithResource = 0,
    PickupResource = 1,
    CarryResourceToCraftingSpot = 2,
    DropResourceInCraftingSpot = 3,
    CraftGood = 4,
    PickupProducedGood = 5,
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
    [SerializeField] StorageSpotData nextCraftingResourceStorageSpotToGetFrom;

#if UNITY_INCLUDE_TESTS
    public CraftingSpotData CraftingSpot => reservedCraftingSpot;
    public StorageSpotData ReservedStorageSpot => reservedStorageSpot;
#endif

    public const float secondsToPickupSourceResource = 0.5f;
    public const float secondsToDropSourceResource = 0.5f;
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
        reservedCraftingSpot = craftingSpot;
        reservedStorageSpot = storageSpotToStoreCraftedItemIn; // will be null for implicit goods
    }

    public override void Start()
    {
        base.Start();

        // Reserve our storage spots with resources that we will consume to craft the good
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
                reserveCraftingResourceStorageSpotForItem(resource.Item);

        // Reserve a spot to do the crafting
        reserveCraftingSpot(reservedCraftingSpot);

        // If the crafted item can be stored (vs e.g. gold), then reserve the spot to store it
        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
            reserveStorageSpot(reservedStorageSpot);

        // Start out walking to the storage spot with the first resource wek'll use for crafting
        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        if (building != Worker.AssignedBuilding) return; // Only care if our building was the one that was destroyed

        switch ((WorkerTask_CraftItemSubstate)substate)
        {
            case WorkerTask_CraftItemSubstate.GotoSpotWithResource:
                // Do nothing
                break;
            case WorkerTask_CraftItemSubstate.PickupResource:
                // Do nothing; haven't fully picked up so it'll just get dropped onto the ground
                break;
            case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                // We're carrying an item.
                break;
            case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                break;
            case WorkerTask_CraftItemSubstate.CraftGood:
                break;
            case WorkerTask_CraftItemSubstate.PickupProducedGood:
                // Just give the item to the worker
                generateCraftedItem();
                break;
        }
        Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        if (building != Worker.AssignedBuilding) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location.WorldLoc - previousWorldLoc;
        else
            Worker.Location.WorldLoc += building.Location.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource: // go to resource spot
                if (MoveTowards(nextCraftingResourceStorageSpotToGetFrom.Location.WorldLoc, distanceMovedPerSecond))
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.PickupResource);
                break;

            case (int)WorkerTask_CraftItemSubstate.PickupResource:
                if (getPercentSubstateDone(secondsToPickupSourceResource) == 1)
                {
                    // Remove the item from the spot (and the game, technically), and unreserve the spot so that it can be used by other Workers
                    unreserveBuildingCraftingResourceSpot(nextCraftingResourceStorageSpotToGetFrom);
                    nextCraftingResourceStorageSpotToGetFrom.ItemContainer.ClearItem();
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                if (MoveTowards(reservedCraftingSpot.Location.WorldLoc, distanceMovedPerSecond))
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot);
                break;

            case (int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                if (getPercentSubstateDone(secondsToDropSourceResource) == 1)
                {
                    if (HasMoreCraftingResourcesToGet())
                    {
                        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
                        gotoSubstate((int)WorkerTask_CraftItemSubstate.GotoSpotWithResource);
                    }
                    else
                        gotoSubstate((int)WorkerTask_CraftItemSubstate.CraftGood);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.CraftGood: // craft
                if (getPercentSubstateDone(secondsToCraft) == 1)
                {
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.PickupProducedGood);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.PickupProducedGood:
                if (getPercentSubstateDone(secondsToPickupCraftedGood) == 1)
                {
                    generateCraftedItem();
                    CompleteTask();

                    // NOTE that completing the task unreserved the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    if (reservedStorageSpot != null) // explicit good
                    {
                        Worker.StorageSpotReservedForItemInHand = reservedStorageSpot;
                        Worker.OriginalPickupItemNeed = Need;
                        reservedStorageSpot.Reservation.ReserveBy(Worker);
                    }
                }
                break;
            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }

    private void generateCraftedItem()
    {
        Debug.Log("todo: consume crafting resources?");
        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
            Worker.AddItemToHands(new ItemData() { DefnId = CraftingItemDefnId });
        else
            Worker.AssignedBuilding.Town.Gold += 100; // implicit good (e.g. gold) - done // todo: hardcoded
    }
}