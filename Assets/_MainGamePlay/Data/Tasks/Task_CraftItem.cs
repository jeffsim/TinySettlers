using System;
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

    public Task_CraftItem(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot) : base(worker, needData)
    {
        CraftingItemDefnId = needData.NeededItem.Id;

        // Reserve storage spots with resources needed for crafting on start
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
                ReserveCraftingResourceStorageSpotForItem(resource.Item, craftingSpot.Location);

        reservedCraftingSpot = ReserveSpotOnStart(craftingSpot);
    }

    public override Subtask GetNextSubtask()
    {
        // example scenario: 2 resources needed for crafting. subtasks:
        // --- transport resouce 1 to crafting spot
        // 0    walk to resource in itemspot 1
        // 1    pickup resource from itemspot 1
        // 2    walk to crafting spot
        // 3    drop resource in crafting spot
        // --- transport resouce 2 to crafting spot
        // 4    walk to resource in itemspot 2
        // 5    pickup resource from itemspot 2
        // 6    walk to crafting spot
        // 7    drop resource in crafting spot
        // --- ready to craft
        // 8    craft item
        // 9    walk to storage spot
        // 10   drop item in storage spot
        int numResourcesForCrafting = itemBeingCrafted.ResourcesNeededForCrafting.Sum(r => r.Count);
        int craftingSubtaskIndex = numResourcesForCrafting * 4;
        if (SubtaskIndex < craftingSubtaskIndex)
        {
            var spotWithResource = (IItemSpotInBuilding)ReservedSpots[SubtaskIndex / 4];
            switch (SubtaskIndex % 4)
            {
                case 0: return new Subtask_WalkToItemSpot(this, spotWithResource);
                case 1: return new Subtask_PickupItemFromItemSpot(this, spotWithResource);
                case 2: return new Subtask_WalkToMultipleItemSpot(this, reservedCraftingSpot);
                case 3: return new Subtask_DropItemInMultipleItemSpot(this, reservedCraftingSpot);
            }
        }
        else if (SubtaskIndex == craftingSubtaskIndex)
            return new Subtask_CraftItem(this, CraftingItemDefnId, reservedCraftingSpot);
        else if (SubtaskIndex == craftingSubtaskIndex + 1 && itemBeingCrafted.GoodType == GoodType.explicitGood)
            return new Subtask_WalkToItemSpot(this, reservedCraftingSpot.Location.GetClosest(reservedCraftingSpot.Building.StorageSpots));
        else if (SubtaskIndex == craftingSubtaskIndex + 2 && itemBeingCrafted.GoodType == GoodType.explicitGood)
            return new Subtask_DropItemInItemSpot(this, reservedCraftingSpot.Location.GetClosest(reservedCraftingSpot.Building.StorageSpots));
        return null;
    }

    public override void AllSubtasksComplete()
    {
        // if explicit item then already dropped in storage spot; if implicit then create now
        if (itemBeingCrafted.GoodType == GoodType.implicitGood)
            Worker.Assignment.AssignedTo.Town.Gold += itemBeingCrafted.BaseSellPrice;
        base.AllSubtasksComplete();
    }

    IItemSpotInBuilding ReserveCraftingResourceStorageSpotForItem(ItemDefn itemDefn, LocationComponent location)
    {
        var spot = Worker.Assignment.AssignedTo.GetClosestUnreservedStorageSpotWithItem(location, itemDefn);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.Assignment.AssignedTo.DefnId);
        ReserveSpot(spot);
        return spot;
    }
}