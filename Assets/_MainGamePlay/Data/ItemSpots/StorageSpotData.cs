using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData
{
    public BuildingData Building;
    [SerializeField] StorageAreaData Area;

    public LocationComponent Location;
    public ReservationComponent Reservation = new();
    public ItemContainerComponent ItemContainer = new();
    public virtual void UpdateWorldLoc() => Location.UpdateWorldLoc();

    public bool IsEmptyAndAvailable => ItemContainer.IsEmpty && !Reservation.IsReserved;

    public StorageSpotData(StorageAreaData area, Vector2 localLoc)
    {
        Area = area;
        Building = area.Building;
        Location = new(area.Location, localLoc);
    }

    internal void OnBuildingDestroyed()
    {
        if (!ItemContainer.IsEmpty)
        {
            var item = ItemContainer.ClearItem();
            Building.Town.AddItemToGround(item, Location);
        }
    }
}

[Serializable]
public class LocationComponent
{
    public LocationComponent ParentLoc;

    [SerializeField] private Vector2 _localLoc;
    public Vector3 LocalLoc
    {
        get => _localLoc;
        set
        {
            _localLoc = value;
            UpdateWorldLoc();
        }
    }

    public Vector3 WorldLoc; // relative to the world
    internal float DistanceTo(LocationComponent location) => Vector2.Distance(WorldLoc, location.WorldLoc);
    
    public LocationComponent() => WorldLoc = Vector2.zero;

    public LocationComponent(LocationComponent parentLoc, Vector2 localLoc)
    {
        ParentLoc = parentLoc;
        LocalLoc = localLoc;
    }

    public void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc;
        if (ParentLoc != null)
            WorldLoc += ParentLoc.WorldLoc;
    }

    internal void SetWorldLoc(LocationComponent location)
    {
        WorldLoc = location.WorldLoc;
    }
}

[Serializable]
public class ReservationComponent : BaseData
{
    public WorkerData ReservedBy;
    public bool IsReserved => ReservedBy != null;

    public void Unreserve()
    {
        Debug.Assert(IsReserved, "Unreserving already unreserved spot (" + InstanceId + ")");
        ReservedBy = null;
    }

    public void ReserveBy(WorkerData worker)
    {
        Debug.Assert(!IsReserved, "Reserving already reserved storage spot (" + InstanceId + ")");
        Debug.Assert(worker != null, "Null reserver (" + InstanceId + ")");

        ReservedBy = worker;
    }
}

[Serializable]
public class ItemContainerComponent : BaseData
{
    public override string ToString() => $"{(Item == null ? "empty" : Item)}";

    public ItemData Item;
    public bool IsEmpty => Item == null;

    public void SetItem(ItemData item)
    {
        Debug.Assert(Item == null, "Adding item when there already is one (" + InstanceId + ")");
        Item = item;
    }

    public ItemData ClearItem()
    {
        var itemToRemove = Item;
        Item = null;
        return itemToRemove;
    }
}