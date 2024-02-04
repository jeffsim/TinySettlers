using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class WorkerData : BaseData, ILocationProvider, IAssignmentProvider
{
    public override string ToString() => Assignment.AssignedTo.Defn.AssignedWorkerFriendlyName + " (" + InstanceId + ")";// + "-" + worker.Data.UniqueId;

    [SerializeField] public LocationComponent Location { get; set; }
    [SerializeField] public AssignmentComponent Assignment { get; set; } = new();
    [SerializeField] public ItemContainerComponent Hands { get; set; } = new();
    [SerializeField] public AIComponent AI { get; set; }

    internal void DropItemOnGround() => Town.AddItemToGround(Hands.ClearItem(), Location);

    public TownData Town;

    public IItemSpotInBuilding StorageSpotReservedForItemInHand;
    public float DistanceToAssignedBuilding => Location.DistanceTo(Assignment.AssignedTo.Location);

    public NeedData OriginalPickupItemNeed;

    public WorkerData(BuildingData buildingToStartIn)
    {
        Location = Utilities.LocationWithinDistance(buildingToStartIn.Location, 1f);

        Town = buildingToStartIn.Town;

        Assignment.AssignTo(buildingToStartIn);
        Assignment.OnAssignedToChanged += () => AI.CurrentTask.Abandon();  // TODO: I *think* this doesn't need to be cleaned up on destroy (?)

        AI = new(this);
    }

    internal void OnNeedBeingMetCancelled()
    {
        throw new NotImplementedException("nyi");
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public void OnBuildingDestroyed(BuildingData building)
    {
        if (StorageSpotReservedForItemInHand.Building == building)
        {
            StorageSpotReservedForItemInHand.Reservation.Unreserve();
            StorageSpotReservedForItemInHand = null;
            OriginalPickupItemNeed = null;
        }

        AI.CurrentTask.OnBuildingDestroyed(building);

        // If we are assigned to the destroyed building, then assign ourselves to the Camp instead
        if (Assignment.AssignedTo == building)
            Assignment.AssignTo(Town.Camp);
    }

    // pass throughs to current task
    public void Update() => AI.Update();
    public void OnBuildingMoved(BuildingData building, LocationComponent previousLoc) => AI.CurrentTask.OnBuildingMoved(building, previousLoc);
    public void OnBuildingPauseToggled(BuildingData building) => AI.CurrentTask.OnBuildingPauseToggled(building);

    internal float GetMovementSpeed()
    {
        // todo: can be modified via e.g. research, town upgrades, ...
        var distanceMovedPerSecond = 5f;
        if (Hands.HasItem)
            distanceMovedPerSecond *= Hands.Item.Defn.CarryingSpeedModifier;
        return distanceMovedPerSecond;
    }

    internal void DropItemInHandInReservedStorageSpot()
    {
        Debug.Assert(StorageSpotReservedForItemInHand != null, "No StorageSpotReservedForItemInHand");
        Debug.Assert(StorageSpotReservedForItemInHand.Building != null, "No ItemInHand");
        Debug.Assert(!StorageSpotReservedForItemInHand.Building.IsDestroyed, "Building destroyed");
        Debug.Assert(Hands.HasItem, "No ItemInHand");

        // This intentionally does not unreserve the reserved storagespot; caller is responsible for doing that
        StorageSpotReservedForItemInHand.ItemContainer.SetItem(Hands.ClearItem());
        StorageSpotReservedForItemInHand.Reservation.Unreserve();
        StorageSpotReservedForItemInHand = null;
    }

    // TODO: Rather than tie following to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
    // worker is assigned to building and/or by worker's defn
    internal bool CanCleanupStorage() => Assignment.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanPickupAbandonedItems() => Assignment.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanGoGetItemsBuildingsWant() => Assignment.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanCraftItems() => Assignment.AssignedTo.Defn.CanCraft;
    internal bool CanSellItems() => Assignment.AssignedTo.Defn.CanSellGoods;
    internal bool CanGatherResource(ItemDefn neededItem) => Assignment.AssignedTo.Defn.CanGatherResources && Assignment.AssignedTo.Defn.GatherableResources.Contains(neededItem);

    public void UnreserveFirstReservedByWorker<T>(List<T> reservablesToCheck) where T : IReservationProvider
    {
        var firstReservedByWorker = reservablesToCheck.FirstOrDefault(reservable => reservable.Reservation.ReservedBy == this);
        if (firstReservedByWorker != null)
        {
            firstReservedByWorker.Reservation.Unreserve();
            return;
        }

        Debug.Assert(false, "Unreserving spot which isn't reserved by Worker");
    }

    public T ReserveFirstReservable<T>(List<T> reservablesToCheck) where T : IReservationProvider
    {
        var firstReservable = reservablesToCheck.First(reservable => !reservable.Reservation.IsReserved);
        if (firstReservable != null)
        {
            firstReservable.Reservation.ReserveBy(this);
            return firstReservable;
        }

        Debug.Assert(false, "Reserving craftingspot but none available");
        return default;
    }

    public void OnDestroyed()
    {
        Assignment.OnDestroyed();
    }
}