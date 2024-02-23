using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Task_CraftItem : Task
{
    public override string ToString() => $"Craft Item {CraftingItemDefnId}";
    public override TaskType Type => TaskType.Task_CraftItem;

    // The item being crafted
    [SerializeField] string CraftingItemDefnId;
    ItemDefn itemBeingCrafted => GameDefns.Instance.ItemDefns[CraftingItemDefnId];

    [SerializeField] public CraftingSpotData reservedCraftingSpot;
    [SerializeField] public StorageSpotData spotForCraftedItem;
    public List<StorageSpotData> CraftingResourceSpots = new();

    public Task_CraftItem(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot) : base(worker, needData)
    {
        CraftingItemDefnId = needData.NeededItem.Id;

        // Reserve storage spots with resources needed for crafting on start
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
                ReserveCraftingResourceStorageSpotForItemOnStart(resource.Item, craftingSpot.Location);

        reservedCraftingSpot = ReserveSpotOnStart(craftingSpot);

        // we'll reserve the resource spot closest to the crafting spot to hold the crafted item
        // Don't actually reserve it now; re-reserve it when we unreserve the resource spot
        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
            spotForCraftedItem = (StorageSpotData)SpotsToReserveOnStart[0]; // for now just use the first.
    }

    public override Subtask GetNextSubtask()
    {
        // example scenario: 2 resources needed for crafting. subtasks:
        // --- transport resouce 1 to crafting spot
        // 0    walk to resource in itemspot 1
        // 1    pickup resource from itemspot 1
        // 2    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 3    walk to crafting spot
        // 4    drop resource in crafting spot
        // --- transport resouce 2 to crafting spot
        // 5    walk to resource in itemspot 2
        // 6    pickup resource from itemspot 2
        // 7    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 8    walk to crafting spot
        // 9    drop resource in crafting spot
        // --- ready to craft
        // 10   craft item
        // 11   walk to storage spot
        // 12   drop item in storage spot
        int numResourcesForCrafting = itemBeingCrafted.ResourcesNeededForCrafting.Sum(r => r.Count);
        int craftingSubtaskIndex = numResourcesForCrafting * 5;
        if (SubtaskIndex < craftingSubtaskIndex)
        {
            var spotWithResource = (IContainerInBuilding)CraftingResourceSpots[SubtaskIndex / 5];
            switch (SubtaskIndex % 5)
            {
                case 0: return new Subtask_WalkToItemSpot(this, spotWithResource);
                case 1: return new Subtask_PickupItemFromItemSpot(this, spotWithResource);
                case 2: return spotWithResource == spotForCraftedItem ? new Subtask_Noop(this) : new Subtask_UnreserveSpot(this, spotWithResource);
                case 3: return new Subtask_WalkToMultipleItemSpot(this, reservedCraftingSpot);
                case 4: return new Subtask_DropItemInMultipleItemSpot(this, reservedCraftingSpot);
            }
        }
        else if (SubtaskIndex == craftingSubtaskIndex)
            return new Subtask_CraftItem(this, CraftingItemDefnId, reservedCraftingSpot);
        else if (SubtaskIndex == craftingSubtaskIndex + 1 && itemBeingCrafted.GoodType == GoodType.explicitGood)
            return new Subtask_WalkToItemSpot(this, spotForCraftedItem);
        else if (SubtaskIndex == craftingSubtaskIndex + 2 && itemBeingCrafted.GoodType == GoodType.explicitGood)
            return new Subtask_DropItemInItemSpot(this, spotForCraftedItem);
        return null;
    }

    public override void AllSubtasksComplete()
    {
        // if explicit item then already dropped in storage spot; if implicit then create now
        if (itemBeingCrafted.GoodType == GoodType.implicitGood)
            Worker.Assignable.AssignedTo.Town.Gold += itemBeingCrafted.BaseSellPrice;
        base.AllSubtasksComplete();
    }

    IContainerInBuilding ReserveCraftingResourceStorageSpotForItemOnStart(ItemDefn itemDefn, Location location)
    {
        var spot = Worker.Assignable.AssignedTo.GetClosestUnreservedStorageSpotWithItemIgnoreList(location, itemDefn, CraftingResourceSpots);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.Assignable.AssignedTo.DefnId);
        ReserveSpotOnStart(spot);
        CraftingResourceSpots.Add(spot);
        return spot;
    }
}