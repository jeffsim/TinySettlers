using System;
using System.Collections.Generic;
using UnityEngine;

public enum NeedType
{
    ClearStorage,
    PickupAbandonedItem,
    GatherResource,
    CraftingOrConstructionMaterial,
    Defend,
    SellGood,

    PersistentBuildingNeed, // e.g. water for farm
    ConstructionWorker,  // to construct a building
    Repair,  // to construct a building
    EntitySelfNeed // e.g.: food (for hunger)
};

public enum NeedState
{
    // for material need for construction:
    //  construction material need is met when: all required items are present or in-transit
    //  constructor entity need is met when:	entity has 'signed up' to meet the need.  Can be present or in-transit
    //  crafting need is met when: 		        all required items are present or in-transit
    unmet,
    met,
    cancelled
}

public enum NeedCoreType
{
    Item,   // A need for an item
    Entity,  // A need for an entity (to do something)
    Building // An inherent need for a building (e.g. cleanup storage)
}

public enum ItemClass
{
    Unset,
    Resource,
    Edible,
    Drinkable,
    Item,
    RefinedMaterial
}

[Serializable]
public class NeedData : BaseData
{
    public override string ToString()
    {
        switch (Type)
        {
            case NeedType.ClearStorage:
                return $"Need: {Type} {BuildingWithNeed} {State} {Priority}";
            case NeedType.PickupAbandonedItem:
                return $"Need: {Type} {AbandonedItemToPickup} {State} {Priority}";
            case NeedType.GatherResource:
                return $"Need: {Type} {NeededItem} {State} {Priority}";
            case NeedType.CraftingOrConstructionMaterial:
                return $"Need: {Type} {NeededItem} {State} {Priority}";
            case NeedType.Defend:
                return $"Need: {Type} {BuildingWithNeed}";
            case NeedType.SellGood:
                return $"Need: {Type} {NeededItem} {State} {Priority}";
            case NeedType.PersistentBuildingNeed:
                return $"Need: {Type} {NeededItem} {State} {Priority}";
            case NeedType.ConstructionWorker:
                return $"Need: {Type} {BuildingWithNeed} {State} {Priority}";
            case NeedType.Repair:
                return $"Need: {Type} {BuildingWithNeed} {State} {Priority}";
            case NeedType.EntitySelfNeed:
                return $"Need: {Type} {NeededItem} {State} {Priority}";
        }
        return $"UNKNOWN NEED TYPE {Type} {NeededItem} {State} {Priority}";
    }
    [SerializeField] bool IsCancelled = false;
    public NeedState State => IsCancelled ? NeedState.cancelled : (IsBeingFullyMet ? NeedState.met : NeedState.unmet);
    public NeedType Type;
    public NeedCoreType NeedCoreType;

    public float StartTimeInSeconds;

    public float Priority; // 0-1, self-determined

    // The Building (if any) that has the Need - e.g. crafting resource
    public BuildingData BuildingWithNeed;

    // The item (if any) that has the need - only for abandoned items (e.g. after a building is destroyed or courier is killed)
    public ItemData AbandonedItemToPickup;

    // The Worker (if any) that has the Need - e.g. hunger
    public WorkerData EntityWithNeed;

    // Can't serialize reference to SOs
    public ItemDefn NeededItem => GameDefns.Instance.ItemDefns[neededItemDefnId];
    [SerializeField] private string neededItemDefnId;

    public ItemClass ItemClass;

    public List<WorkerData> WorkersMeetingNeed;

    public int MaxNumWorkersThatCanMeetNeed;
    public bool IsBeingFullyMet => WorkersMeetingNeed.Count == MaxNumWorkersThatCanMeetNeed;

    public NeedData(BuildingData buildingData, NeedType needType)
    {
        BuildingWithNeed = buildingData;
        Type = needType;
        ItemClass = ItemClass.Unset;
        Priority = 0;//.5f; // TODO: Remove
        WorkersMeetingNeed = new List<WorkerData>();
        StartTimeInSeconds = GameTime.time;
    }

    public NeedData(ItemData abandonedItem)
    {
        AbandonedItemToPickup = abandonedItem;
        Type = NeedType.PickupAbandonedItem;
        Priority = 0.1f;//.5f; // TODO: Remove
        WorkersMeetingNeed = new List<WorkerData>();
        StartTimeInSeconds = GameTime.time;
        MaxNumWorkersThatCanMeetNeed = 1;
    }

    public NeedData(BuildingData buildingData, NeedType needType, ResourceNeededForCraftingOrConstruction resource) : this(buildingData, needType)
    {
        neededItemDefnId = resource.Item.Id;
        MaxNumWorkersThatCanMeetNeed = resource.Count;
    }

    public NeedData(BuildingData buildingData, NeedType needType, ItemDefn item = null, int numNeeded = 1) : this(buildingData, needType)
    {
        neededItemDefnId = item.Id;
        MaxNumWorkersThatCanMeetNeed = numNeeded;
    }

    internal void AssignWorkerToMeetNeed(WorkerData worker)
    {
        Debug.Assert(!IsBeingFullyMet, "Assigning worker to a need that is already fully met");
        // worker.AssignToMeetNeed(this);
        WorkersMeetingNeed.Add(worker);
    }

    internal void UnassignWorkerToMeetNeed(WorkerData worker)
    {
        WorkersMeetingNeed.Remove(worker);
    }

    internal void Cancel()
    {
        Debug.Assert(!IsCancelled, "Cancelling already cancelled Need");
        foreach (var worker in WorkersMeetingNeed)
            worker.OnNeedBeingMetCancelled();
        IsCancelled = true;
    }
}