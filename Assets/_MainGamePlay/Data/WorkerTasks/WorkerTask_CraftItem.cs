using System;
using System.Collections.Generic;
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
    [SerializeField] StorageSpotData storageSpotForCraftedGood;
    [SerializeField] StorageSpotData nextCraftingResourceStorageSpotToGetFrom;

#if UNITY_INCLUDE_TESTS
    public CraftingSpotData CraftingSpot => reservedCraftingSpot;
#endif

    public const float secondsToPickupSourceResource = 0.5f;
    public const float secondsToDropSourceResource = 0.5f;
    public const float secondsToCraft = 1;
    public const float secondsToPickupCraftedGood = 0.5f;

    [SerializeField] string lastPickedUpResourceDefnId;

    public override bool IsWalkingToTarget => substate == (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource || substate == (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot;

    public override ItemDefn GetTaskItem()
    {
        var craftingItem = GameDefns.Instance.ItemDefns[CraftingItemDefnId];
        switch ((WorkerTask_CraftItemSubstate)substate)
        {
            case WorkerTask_CraftItemSubstate.GotoSpotWithResource: return nextCraftingResourceStorageSpotToGetFrom.ItemContainer.Item.Defn;
            case WorkerTask_CraftItemSubstate.PickupResource: return nextCraftingResourceStorageSpotToGetFrom.ItemContainer.Item.Defn;
            case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot: return GameDefns.Instance.ItemDefns[lastPickedUpResourceDefnId];
            case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot: return GameDefns.Instance.ItemDefns[lastPickedUpResourceDefnId];
            case WorkerTask_CraftItemSubstate.CraftGood: return craftingItem;
            case WorkerTask_CraftItemSubstate.PickupProducedGood: return craftingItem;
            default: return null;
        }
    }

    public override string ToDebugString()
    {
        var str = "Craft item\n";
        str += "  substate: " + substate;
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_CraftItem Create(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot)
    {
        return new(worker, needData, craftingSpot);
    }

    private WorkerTask_CraftItem(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot) : base(worker, needData)
    {
        CraftingItemDefnId = needData.NeededItem.Id;
        reservedCraftingSpot = ReserveSpotOnStart(craftingSpot);
    }

    public override void Start()
    {
        base.Start();

        // Reserve our storage spots with resources that we will consume to craft the good
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
                reserveCraftingResourceStorageSpotForItem(resource.Item, reservedCraftingSpot.Location);

        // Determine the resource spot that is closest to the crafting spot; we'll keep the reservation for that spot until
        // the end of the task so that it can be used to store the crafted item
        storageSpotForCraftedGood = reservedCraftingSpot.Location.GetClosest(ReservedCraftingResourceStorageSpots);

        // Start out walking to the storage spot with the first resource we'll use for crafting
        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
    }

    // Note: this is called when any building is destroyed, not just "this task's" building
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
                // An alternative I considered was to have the worker continue picking up the item so that they could
                // then carry it to another storage spot; that would avoid the item being dropped on the ground and immediately
                // re-picked up; but it's an edge case and I don't want to complicate the code for it.
                break;

            case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                // We're carrying an item.
                break;

            case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                // We're currently dropping a resource into the Crafting Spot.
                break;

            case WorkerTask_CraftItemSubstate.CraftGood:
                // We're currently crafting a good from the resources that have already been carried to the crafting spot
                break;

            case WorkerTask_CraftItemSubstate.PickupProducedGood:
                // We're picking up the good that we just crafted.
                // Just give the item to the worker
                generateCraftedItem();
                break;
        }
        Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        if (building != Worker.AssignedBuilding) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location - previousLoc;
        else
            Worker.Location += building.Location - previousLoc;
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        // TODO (FUTURE): This causes the player to lose items if the building is paused e.g. mid-crafting.  Not ideal, but will accept for now to keep code simpler
        if (building == Worker.AssignedBuilding)
            Abandon();
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_CraftItemSubstate.GotoSpotWithResource: // go to resource spot
                if (MoveTowards(nextCraftingResourceStorageSpotToGetFrom.Location, distanceMovedPerSecond))
                    GotoSubstate((int)WorkerTask_CraftItemSubstate.PickupResource);
                break;

            case (int)WorkerTask_CraftItemSubstate.PickupResource:
                if (IsSubstateDone(secondsToPickupSourceResource))
                {
                    // Remove the item from the spot (and the game, technically), and unreserve the spot so that it can be used by other Workers
                    unreserveBuildingCraftingResourceSpot(nextCraftingResourceStorageSpotToGetFrom);

                    // Note: retain the last spot since we'll use it to transport the crafted good to.
                    if (nextCraftingResourceStorageSpotToGetFrom == storageSpotForCraftedGood)
                        storageSpotForCraftedGood.Reservation.ReserveBy(Worker);

                    lastPickedUpResourceDefnId = nextCraftingResourceStorageSpotToGetFrom.ItemContainer.Item.DefnId;
                    nextCraftingResourceStorageSpotToGetFrom.ItemContainer.ClearItem();
                    GotoSubstate((int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                if (MoveTowards(reservedCraftingSpot.Location, distanceMovedPerSecond))
                    GotoSubstate((int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot);
                break;

            case (int)WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                if (IsSubstateDone(secondsToDropSourceResource))
                {
                    // The resource has been dropped.  We don't actually put the resource anywhere and act like it's been 'consumed'. 
                    // so it disappears from the game automatically.
                    if (HasMoreCraftingResourcesToGet())
                    {
                        nextCraftingResourceStorageSpotToGetFrom = getNextReservedCraftingResourceStorageSpot();
                        GotoSubstate((int)WorkerTask_CraftItemSubstate.GotoSpotWithResource);
                    }
                    else
                        GotoSubstate((int)WorkerTask_CraftItemSubstate.CraftGood);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.CraftGood: // craft
                if (IsSubstateDone(secondsToCraft))
                {
                    GotoSubstate((int)WorkerTask_CraftItemSubstate.PickupProducedGood);
                }
                break;

            case (int)WorkerTask_CraftItemSubstate.PickupProducedGood:
                if (IsSubstateDone(secondsToPickupCraftedGood))
                {
                    generateCraftedItem();
                    CompleteTask();

                    // NOTE that completing the task unreserved the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    if (itemBeingCrafted.GoodType == GoodType.explicitGood)
                    {
                        Worker.StorageSpotReservedForItemInHand = storageSpotForCraftedGood;
                        Worker.OriginalPickupItemNeed = Need;
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
        // Crafting resources were silently/automatically consumed above when we cleared them from their storagespots during pickup
        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
            Worker.AddItemToHands(new ItemData() { DefnId = CraftingItemDefnId });
        else
            Worker.AssignedBuilding.Town.Gold += 100; // implicit good (e.g. gold) - done // todo: hardcoded
    }

    bool HasMoreCraftingResourcesToGet()
    {
        return ReservedCraftingResourceStorageSpots.Count > 0;
    }

    StorageSpotData reserveCraftingResourceStorageSpotForItem(ItemDefn itemDefn, LocationComponent location)
    {
        var spot = Worker.AssignedBuilding.GetClosestUnreservedStorageSpotWithItem(location, itemDefn, out float _);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.AssignedBuilding.DefnId);

        spot.Reservation.ReserveBy(Worker);
        ReservedCraftingResourceStorageSpots.Add(spot);
        return spot;
    }

    void unreserveBuildingCraftingResourceSpot(StorageSpotData spot)
    {
        spot.Reservation.Unreserve();
        ReservedCraftingResourceStorageSpots.Remove(spot);
    }

    StorageSpotData getNextReservedCraftingResourceStorageSpot()
    {
        Debug.Assert(ReservedCraftingResourceStorageSpots.Count > 0, "getting crafting resource spot, but none remain");
        return ReservedCraftingResourceStorageSpots[0];
    }
}