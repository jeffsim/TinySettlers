using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingCraftingMgr
{
    public List<CraftingSpotData> CraftingSpots = new();

    public int NumReservedCraftingSpots
    {
        get
        {
            // TODO (PERF): Cache
            int count = 0;
            foreach (var spot in CraftingSpots) if (spot.Reservation.IsReserved) count++;
            return count;
        }
    }
    public int NumAvailableCraftingSpots => Building.Defn.CraftingSpots.Count - NumReservedCraftingSpots;
    public bool HasAvailableCraftingSpot => NumAvailableCraftingSpots > 0;

    public BuildingData Building;

    public BuildingCraftingMgr(BuildingData building)
    {
        Building = building;
        if (!Building.Defn.CanCraft)
            return;

        for (int i = 0; i < Building.Defn.CraftingSpots.Count; i++)
            CraftingSpots.Add(new(Building, i));

        // Create needs for all craftables; TBD if priorities are set assuming all are crafted, or if only some are prioritized
        // based on AI (ala Settlers) or human selection.
        foreach (var item in Building.Defn.CraftableItems)
        {
            // Add Need for "I need to craft (if resources are in room)"
            Building.Needs.Add(new NeedData(Building, NeedType.CraftGood, item) { NeedCoreType = NeedCoreType.Building });

            // Add Need for "I need resources to craft"
            // TODO: If two craftable items use the same resource, then we'll have two needs for that resource.  sum up counts
            foreach (var resource in item.ResourcesNeededForCrafting)
                Building.ItemNeeds.Add(new NeedData(Building, NeedType.CraftingOrConstructionMaterial, resource.Item, resource.Count));
        }
        Building.Needs.AddRange(Building.ItemNeeds);
    }

    public CraftingSpotData ReserveCraftingSpot(WorkerData worker) => worker.ReserveFirstReservable(CraftingSpots);

    public void UnreserveCraftingSpot(WorkerData worker) => worker.UnreserveFirstReservedByWorker(CraftingSpots);

    public void UpdateWorldLoc()
    {
        foreach (var spot in CraftingSpots) spot.UpdateWorldLoc();
    }

    public CraftingSpotData GetAvailableCraftingSpot()
    {
        foreach (var spot in CraftingSpots)
            if (!spot.Reservation.IsReserved)
                return spot;
        return null;
    }

    public void Destroy()
    {
        foreach (var spot in CraftingSpots) spot.OnBuildingDestroyed();
    }
}
