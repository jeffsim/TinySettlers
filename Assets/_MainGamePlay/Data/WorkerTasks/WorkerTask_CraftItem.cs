using System;
using UnityEngine;

[Serializable]
public class WorkerTask_CraftItem : WorkerTask
{
    public override string ToString() => $"Craft Item {CraftingItemDefnId}";
    public override TaskType Type => TaskType.CraftGood;

    // The id of the item being crafted
    [SerializeField] string CraftingItemDefnId;
    ItemDefn itemBeingCrafted => GameDefns.Instance.ItemDefns[CraftingItemDefnId];

    [SerializeField] public CraftingSpotData reservedCraftingSpot;

    public WorkerTask_CraftItem(WorkerData worker, NeedData needData, CraftingSpotData craftingSpot) : base(worker, needData)
    {
        CraftingItemDefnId = needData.NeededItem.Id;
        reservedCraftingSpot = ReserveSpotOnStart(craftingSpot);
    }

    public override void InitializeStateMachine()
    {
        // Transfer resources from our building's storage to the crafting spot
        foreach (var resource in itemBeingCrafted.ResourcesNeededForCrafting)
            for (int i = 0; i < resource.Count; i++)
            {
                var resourceSpot = ReserveCraftingResourceStorageSpotForItem(resource.Item, reservedCraftingSpot.Location);
                Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, resourceSpot));
                Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, resourceSpot));
                Subtasks.Add(new WorkerSubtask_WalkToMultipleItemSpot(this, reservedCraftingSpot));
                Subtasks.Add(new WorkerSubtask_DropItemInMultipleItemSpot(this, reservedCraftingSpot));
            }

        // craft the item
        Subtasks.Add(new WorkerSubtask_CraftItem(this, CraftingItemDefnId, reservedCraftingSpot));

        if (itemBeingCrafted.GoodType == GoodType.explicitGood)
        {
            // we'll store the crafted good in the same spot that the closest resource was stored in
            // TODO: Ensure I handle unreserving properly below
            var storageSpotForCraftedGood = reservedCraftingSpot.Location.GetClosest(reservedCraftingSpot.Building.StorageSpots);
            Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, storageSpotForCraftedGood));
            Subtasks.Add(new WorkerSubtask_DropItemInItemSpot(this, storageSpotForCraftedGood));
        }
    }

    public override void AllSubtasksComplete()
    {
        // if explicit item then already dropped in storage spot; if implicit then create now
        Worker.Assignment.AssignedTo.Town.Gold += itemBeingCrafted.BaseSellPrice; // implicit good (e.g. gold)

        CompleteTask();
    }

    IItemSpotInBuilding ReserveCraftingResourceStorageSpotForItem(ItemDefn itemDefn, LocationComponent location)
    {
        var spot = Worker.Assignment.AssignedTo.GetClosestUnreservedStorageSpotWithItem(location, itemDefn);
        Debug.Assert(spot != null, "Failed to find spot with unreserved item " + itemDefn.Id + " in " + Worker.Assignment.AssignedTo.DefnId);
        ReserveSpot(spot);
        return spot;
    }
}