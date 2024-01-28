using System;
using UnityEngine;

public enum WorkerTask_CraftItemSubstate
{
    GotoSpotWithResource = 0,
    PickupResource = 1,
    CarryResourceToCraftingSpot = 2,
    DropResourceInCraftingSpot = 3,
    ProduceGood = 4,
    CarryCraftedGoodToStorageSpot = 5, // only used if == explicit item (e.g. wood, not gold)
    DropCraftedGoodInStorageSpot = 6,  // only used if == explicit item (e.g. wood, not gold)
};

[Serializable]
public class WorkerTask_CraftItem : WorkerTask
{
    public override string ToString() => "Craft good (" + CraftingItemDefnId + ")";
    public override TaskType Type => TaskType.CraftGood;

    // Lookup.  TODO: Cache.
    ItemDefn itemBeingCrafted => GameDefns.Instance.ItemDefns[CraftingItemDefnId];

    // The id of the item being crafted
    [SerializeField] string CraftingItemDefnId;

    // Where the crafted good will be stored.  Only used if crafted item is explicit item
    [SerializeField] StorageSpotData reservedStorageSpot;

    // The spot in which the crafting will be done
    [SerializeField] CraftingSpotData reservedCraftingSpot;

    [SerializeField] StorageSpotData nextCraftingResourceStorageSpotToGetFrom;

    public const float secondsToPickup = .5f;
    public const float secondsToCraft = 1;
    public const float secondsToDrop = 0.5f;

    // Used to draw walking lines in debug mode
    public override bool IsWalkingToTarget =>
        substate == (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource ||
        substate == (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot ||
        substate == (int)WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot;

    public override ItemDefn GetTaskItem()
    {
        if (string.IsNullOrEmpty(CraftingItemDefnId)) return null;
        return GameDefns.Instance.ItemDefns[CraftingItemDefnId];
    }
    internal override string getDebuggerString()
    {
        return "Crafting";
    }

    public override string ToDebugString()
    {
        var str = "Craft\n";
        str += "  Item: " + CraftingItemDefnId + "\n";
        str += "  TargetStorage: " + reservedStorageSpot.InstanceId + " (" + reservedStorageSpot.Building.DefnId + ")\n";
        str += "  CraftingSpot: " + reservedCraftingSpot.InstanceId + " (" + reservedCraftingSpot.Building.DefnId + ")\n";
        str += "  NextSourceSpot: " + nextCraftingResourceStorageSpotToGetFrom.InstanceId + " (" + nextCraftingResourceStorageSpotToGetFrom.Building.DefnId + ")\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, nextCraftingResourceStorageSpotToGetFrom.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_CraftItemSubstate.PickupResource: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            case (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, reservedCraftingSpot.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            case (int)WorkerTask_CraftItemSubstate.ProduceGood: str += "; per = " + getPercentSubstateDone(secondsToCraft); break;
            case (int)WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, reservedStorageSpot.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_CraftItemSubstate.DropCraftedGoodInStorageSpot: str += "; per = " + getPercentSubstateDone(secondsToDrop); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    public override bool IsCarryingItem(string itemId)
    {
        return substate > 4 && CraftingItemDefnId == itemId;
    }

    // TODO: Pooling
    public static WorkerTask_CraftItem Create(WorkerData worker, ItemDefn itemToCraft)
    {
        return new WorkerTask_CraftItem(worker, itemToCraft);
    }

    private WorkerTask_CraftItem(WorkerData worker, ItemDefn itemToCraft) : base(worker)
    {
        CraftingItemDefnId = itemToCraft.Id;
    }

    public override void Start()
    {
        base.Start();

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

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        if (building != Worker.AssignedBuilding) return;

        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        // Note: can't always just move our world loc if our building moved, because we may be moving to the
        // building (e.g. immediately after being assigned to it)
        WorkerTask_CraftItemSubstate s = (WorkerTask_CraftItemSubstate)substate;
        if (s == WorkerTask_CraftItemSubstate.GotoSpotWithResource ||
            s == WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot ||
            s == WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot)
        {
            LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        }
        else if (s == WorkerTask_CraftItemSubstate.PickupResource ||
                 s == WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot ||
                 s == WorkerTask_CraftItemSubstate.ProduceGood ||
                 s == WorkerTask_CraftItemSubstate.DropCraftedGoodInStorageSpot)
        {
            Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
        }
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource:
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

            case (int)WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot: // Walk to the spot that will store the crafted good
                if (MoveTowards(reservedStorageSpot.WorldLoc, distanceMovedPerSecond))
                    gotoSubstate((int)WorkerTask_CraftItemSubstate.DropCraftedGoodInStorageSpot);
                break;

            case (int)WorkerTask_CraftItemSubstate.DropCraftedGoodInStorageSpot: // drop the crafted item; and then done.
                if (getPercentSubstateDone(secondsToDrop) == 1)
                {
                    // Done dropping.  Add the item into the storage spot.  Complete the task first so that the spot is unreserved so that we can add to it
                    CompleteTask();
                    Worker.AssignedBuilding.AddItemToItemSpot(new ItemData() { DefnId = CraftingItemDefnId }, reservedStorageSpot);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}