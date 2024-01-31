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
    public virtual void UpdateWorldLoc()
    {
        Location.WorldLoc = Area.Location.WorldLoc + Location.LocalLoc;
    }

    public bool IsEmptyAndAvailable => ItemContainer.IsEmpty && !Reservation.IsReserved;

    public StorageSpotData(StorageAreaData area, Vector2 localLoc)
    {
        Area = area;
        Building = area.Building;
        Location = new LocationComponent(area.Location, localLoc.x, localLoc.y);
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
    public override string ToString() => WorldLoc.ToString();
    public LocationComponent ParentLoc;

    public Vector3 LocalLoc;
    public Vector3 WorldLoc;

    public LocationComponent() => WorldLoc = Vector2.zero;
    public LocationComponent(LocationComponent sourceLoc) => WorldLoc = sourceLoc.WorldLoc;
    public LocationComponent(Vector3 worldLoc) => WorldLoc = worldLoc;
    public LocationComponent(Vector2 worldLoc) => WorldLoc = worldLoc;
    public LocationComponent(float worldX, float worldY) => WorldLoc = new(worldX, worldY);

    internal float DistanceTo(LocationComponent location) => Vector2.Distance(WorldLoc, location.WorldLoc);

    public static LocationComponent operator -(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc - loc2.WorldLoc);
    public static LocationComponent operator +(LocationComponent loc1, LocationComponent loc2) => new(loc1.WorldLoc + loc2.WorldLoc);

    internal void SetWorldLoc(LocationComponent location) => WorldLoc = location.WorldLoc;
    internal void SetWorldLoc(float x, float y) => WorldLoc.Set(x, y, 0);

    public LocationComponent(LocationComponent parentLoc, float localX, float localY)
    {
        LocalLoc = new(localX, localY);
        ParentLoc = parentLoc;
        UpdateWorldLoc();
    }

    internal void MoveTowards(LocationComponent loc1, LocationComponent loc2, float t)
    {
        WorldLoc = Vector2.MoveTowards(loc1.WorldLoc, loc2.WorldLoc, t);
    }

    public void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc;
        if (ParentLoc != null)
            WorldLoc += ParentLoc.WorldLoc;
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