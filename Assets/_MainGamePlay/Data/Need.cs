using System;
using System.Collections.Generic;
using UnityEngine;

public enum NeedType
{
    ClearStorage,
    GatherResource,
    CraftingOrConstructionMaterial,
    Defend,

    PersistentRoomNeed, // e.g. water for farm
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
    canspoted
}

public enum NeedCoreType
{
    Item,   // A need for an item
    Entity,  // A need for an entity (to do something)
    Building // An inherent needf for a building (e.g. cleanup storage)
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
    [SerializeField] bool IsCanspoted = false;
    public NeedState State => IsCanspoted ? NeedState.canspoted : (IsBeingFullyMet ? NeedState.met : NeedState.unmet);
    public NeedType Type;
    public NeedCoreType NeedCoreType;

    public float StartTimeInSeconds;

    public float Priority; // 0-1, self-determined

    // The Building (if any) that has the Need - e.g. crafting resource
    public BuildingData BuildingWithNeed;

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

    internal void Cancel()
    {
        Debug.Assert(!IsCanspoted, "Canspoting already canspoted Need");
        foreach (var worker in WorkersMeetingNeed)
            worker.OnNeedBeingMetCanspoted();
        IsCanspoted = true;
    }

    // itemCellDistance = distance from mob to cell that the specified item is in.
    public float PriorityOfMeetingItemNeed(WorkerData entity, BuildingData building, float itemCellDistance)
    {
        // DebugMgr.AssertNotNull(entity, "Passed null entity to PriorityOfMeetingNeed");
        // DebugMgr.AssertNotNull(room, "Passed null room to PriorityOfMeetingNeed");
        // DebugMgr.Assert(NeedType != NeedType.Construction, "should only be called for item needs (persistent item, crafting, construction, or entityself)");

        var distFromResourcesRoomToRoom = building.getDistanceToBuilding(entity.AssignedBuilding) * 9;
        var totalDistance = itemCellDistance + distFromResourcesRoomToRoom;
        var timeNeedHasBeenAlive = GameTime.time - this.StartTimeInSeconds;

        // TODO: I'm sure this will need tweaking...
        return this.Priority * 100 + (30 - (Math.Min(30, totalDistance))) / 10f + timeNeedHasBeenAlive / 10f;
    }
}