using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class WorkerData : BaseData, ILocation, IAssignable, IOccupier, IExhaustible
{
    public override string ToString() => Assignable.AssignedTo.Defn.AssignedWorkerFriendlyName + " (" + InstanceId + ")";// + "-" + worker.Data.UniqueId;

    private WorkerDefn _defn;
    public WorkerDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.WorkerDefns[DefnId];
    public string DefnId;

    [SerializeField] public Location Location { get; set; }
    [SerializeField] public Container Hands { get; set; }
    [SerializeField] public AIComponent AI { get; set; }
    [SerializeField] public Assignable Assignable { get; set; }
    [SerializeField] public Occupier Occupier { get; set; }
    [SerializeField] public Exhaustible Exhaustible { get; set; }

    public TownData Town;
    public NeedData OriginalPickupItemNeed;

    public WorkerData(WorkerDefn defn, BuildingData buildingToStartIn)
    {
        DefnId = defn.Id;
        Town = buildingToStartIn.Town;

        Location = Utilities.LocationWithinDistance(buildingToStartIn.Location, 1f);
        Location.WorldLoc.y = Settings.Current.WorkerY;

        Occupier = new(this);
        Assignable = new(this);
        Exhaustible = new(this);
        Hands = new();
        Assignable.AssignTo(buildingToStartIn);
        AI = new(this);
    }

    public void OnAssignedToChanged()
    {
        AI?.CurrentTask.Abandon();
    }

    internal void OnNeedBeingMetCancelled()
    {
        // throw new NotImplementedException("nyi");
    }

    // Called when any building is destroyed; if this Task involves that building then determine
    // what we should do (if anything).
    public void OnBuildingDestroyed(BuildingData building)
    {
        AI.CurrentTask.OnBuildingDestroyed(building);

        // If we are assigned to the destroyed building, then assign ourselves to the Camp instead
        if (Assignable.AssignedTo == building)
            Assignable.AssignTo(Town.Camp);
    }

    // pass throughs to current task
    public void Update()
    {
        AI.Update();
        Exhaustible.Update();
    }

    public void OnBuildingMoved(BuildingData building, Location previousLoc) => AI.CurrentTask.OnBuildingMoved(building, previousLoc);
    public void OnBuildingPauseToggled(BuildingData building) => AI.CurrentTask.OnBuildingPauseToggled(building);

    internal float GetMovementSpeed()
    {
        // todo: can be modified via e.g. research, town upgrades, ...
        var distanceMovedPerSecond = 2.5f;
        if (Hands.HasItem)
            distanceMovedPerSecond *= Hands.FirstItem.Defn.CarryingSpeedModifier;
        return distanceMovedPerSecond;
    }

    internal void DropItemInHandInSpot(IContainerInBuilding spot)
    {
        Debug.Assert(Hands.HasItem, "No ItemInHand");

        // This intentionally does not unreserve the reserved storagespot; caller is responsible for doing that
        spot.Container.AddItem(Hands.ClearItems());
        // spot.Reservation.Unreserve();
    }

    // TODO: Rather than tie following to AssignedBuilding, make it an attribute of the Worker which is assigned as bitflag; bitflag is set when
    // worker is assigned to building and/or by worker's defn
    internal bool CanCleanupStorage() => Assignable.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanPickupAbandonedItems() => Assignable.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanGoGetItemsBuildingsWant() => Assignable.AssignedTo.Defn.WorkersCanFerryItems;
    internal bool CanCraftItems() => Assignable.AssignedTo.Defn.CanCraft;
    internal bool CanSellItems() => Assignable.AssignedTo.Defn.CanSellGoods;
    internal bool CanGatherResource(ItemDefn neededItem) => Assignable.AssignedTo.Defn.CanGatherResources && Assignable.AssignedTo.Defn.GatherableResources.Contains(neededItem);

    public void UnreserveFirstReservedByWorker<T>(List<T> reservablesToCheck) where T : IReservable
    {
        var firstReservedByWorker = reservablesToCheck.FirstOrDefault(reservable => reservable.Reservable.ReservedBy == this);
        if (firstReservedByWorker != null)
        {
            firstReservedByWorker.Reservable.Unreserve();
            return;
        }

        Debug.Assert(false, "Unreserving spot which isn't reserved by Worker");
    }

    public T ReserveFirstReservable<T>(List<T> reservablesToCheck) where T : IReservable
    {
        var firstReservable = reservablesToCheck.First(reservable => !reservable.Reservable.IsReserved);
        if (firstReservable != null)
        {
            firstReservable.Reservable.ReserveBy(this);
            return firstReservable;
        }

        Debug.Assert(false, "Reserving craftingspot but none available");
        return default;
    }

    public void OnDestroyed()
    {
        Assignable.OnDestroyed();
    }

    internal void DropItemOnGround()
    {
        Town.AddItemToGround(Hands.ClearItems(), Location);
    }
}