using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BuildingData : BaseData, ILocation, IOccupiable, IConstructable, IPausable
{
    public List<NeedData> ConstructionNeeds = new();

    public List<NeedData> Needs = new();

    // If this building can craft items or sell items, then ItemNeeds contains the priority of
    // how much we need each of those items.  priority is dependent on how many are in storage.
    public List<NeedData> ItemNeeds = new();

    // How badly we need a courier to clear out our storage
    public NeedData ClearOutStorageNeed;

    // If this building can gather resources, then GatheringNeeds contains the priority of
    // gathering each resource.  e.g.:
    // * if we have many of X and few of Y, then Y may have a higher priority
    // * if our storage is nearly full then all resource gathering is at a reduced priority
    // * TODO: If another building has broadcast a need for resource R and we can gather it, then
    //   increase priority to gather it.  note that a settlers-like model may ONLY do these.
    public List<NeedData> GatheringNeeds = new();

    public bool NeedsItemForSelf(ItemDefn itemDefn) => ItemNeeds.Find(need => need.NeededItem == itemDefn) != null;
    public bool HasUnreservedItemOfTypeAndDoesntNeedIt(ItemDefn itemDefn) => StorageSpots.Any(spot => !spot.Reservable.IsReserved && spot.Container.ContainsItem(itemDefn) && !NeedsItemForSelf(itemDefn));


    public void UpdateNeedPriorities()
    {
        int totalNumStorageSpot = StorageAreas.Sum(area => area.NumStorageSpots);

        float percentFull = 1 - (float)NumAvailableStorageSpots / totalNumStorageSpot;

        if (Defn.CanStoreItems)
        {
            if (Defn.IsPrimaryStorage)
                ClearOutStorageNeed.Priority = 0;
            else
            {
                // unless close to full, Cleanup tasks are lower priority than filling crafting need tasks
                ClearOutStorageNeed.Priority = percentFull / 10f;

                // if we're a crafting building then we have a higher priority than non-crafting buildings (e.g. woodcutter) to clear storage
                // so that we can craft more
                if (Defn.CanCraft)
                    ClearOutStorageNeed.Priority *= 1.5f;
            }
        }

        foreach (var need in ItemNeeds)
        {
            if (IsPaused)
            {
                need.Priority = 0;
                continue;
            }
            need.Priority = 1;
            float storageOccupancyRatio = (float)NumItemsInStorage / totalNumStorageSpot;
            float fullnessAdjustment = percentFull > 0.99f ? 0 :
                                       percentFull > 0.75f ? 0.5f :
                                       percentFull > 0.5f ? 0.75f :
                                       percentFull > 0.25f ? 0.875f :
                                       percentFull > 0.1f ? 0.9f : 1f;
            float occupancyAdjustment = storageOccupancyRatio > 0.5f ? 0.5f :
                                        storageOccupancyRatio > 0.25f ? 0.75f : 1f;

            need.Priority *= Math.Min(fullnessAdjustment, occupancyAdjustment);
        }

        foreach (var need in Needs)
        {
            if (need.IsBeingFullyMet || IsPaused)
            {
                need.Priority = 0;
                continue;
            }

            if (need.Type == NeedType.CraftGood)
            {
                // Priority of Crafting is set by:
                //  does anyone else need it and how badly (priority of need)
                //  how many of the item are already in the Town?
                //  do we want/need to sell the item?
                var globalPriorityOfNeedForItem = 0f;
                foreach (var building in Town.AllBuildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == need.NeededItem)
                            globalPriorityOfNeedForItem += otherNeed.Priority;

                var storageImpact = Mathf.Clamp(Town.NumTotalItemsInStorage(need.NeededItem) / 10f, 0, 2);

                var priorityToSellItem = 0f;
                foreach (var building in Town.AllBuildings)
                    foreach (var otherNeed in building.Needs)
                        if (otherNeed.Type == NeedType.SellItem && otherNeed.NeededItem == need.NeededItem)
                            priorityToSellItem += otherNeed.Priority;

                need.Priority = storageImpact / 2f + globalPriorityOfNeedForItem + priorityToSellItem + .3f;
                need.Priority = 2;
            }

            if (need.Type == NeedType.SellItem)
            {
                // The need to sell (for the worker) is always 1.  Howver, the building's need for the item
                // to be sold is based on how many are in storage and how badly other buildings need it
                need.Priority = 2;

                // Get the ItemNeed for this item to sell
                var itemNeed = ItemNeeds.Find(n => n.NeededItem == need.NeededItem);
                Debug.Assert(itemNeed != null, "ItemNeed not found for item to sell");

                var item = itemNeed.NeededItem;
                var globalNeedForItem = 0f;

                // if the item-to-be-sold is highly needed by other buildings, then don't sell it
                foreach (var building in Town.AllBuildings)
                {
                    if (building == this) continue;
                    foreach (var otherNeed in building.Needs)
                    {
                        if (otherNeed.Type == NeedType.CraftingOrConstructionMaterial && otherNeed.NeededItem == item)
                            globalNeedForItem += otherNeed.Priority;
                        // if (otherNeed.Type == NeedType.ClearStorage && building.NumItemsInStorage(item) > 0) // todo: not quite right; only 1 of need's item is in storage will be the smae priority as if 9 of needs' item are in storage
                        // globalNeedForItem += otherNeed.Priority;
                        //   numInStorage += building.NumItemsInStorage(item); // doesn't include ferrying items but :shrug:
                    }
                }
                // if (globalNeedForItem > 0.5f) // TODO: Allow user to modify this to e.g. effect a 'fire sale' in which even highly needed items are sold
                // {
                //     need.Priority = 0;
                //     continue;
                // }

                // if here then the item-to-be-sold isn't highly needed.  If there's a lot of it in storage, then sell it
                int numInStorage = Town.NumTotalItemsInStorage(item);
                var storageImpact = Mathf.Clamp(numInStorage / 10f, 0, 2);
                itemNeed.Priority = storageImpact / 2f + .2f;
            }
        }
        foreach (var need in GatheringNeeds)
        {
            if (IsPaused)
            {
                need.Priority = 0;
                continue;
            }
            need.Priority = .1f;
            // if we have a lot of them then reduce priority
            int numOfNeededItemAlreadyInStorage = NumItemsOfTypeInStorage(need.NeededItem);
            numOfNeededItemAlreadyInStorage = Town.NumTotalItemsInStorage(need.NeededItem);

            if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 2)
                need.Priority *= .5f; // storage is half+ full of the needed item
            else if (numOfNeededItemAlreadyInStorage > Town.NumTotalStorageSpots() / 4)
                need.Priority *= .75f; // storage is 25%-50% full of the needed item
        }

    }

    private NeedData getItemNeedForItem(ItemDefn defn)
    {
        foreach (var need in ItemNeeds)
            if (need.NeededItem.Id == defn.Id)
                return need;
        return null;
    }
}
