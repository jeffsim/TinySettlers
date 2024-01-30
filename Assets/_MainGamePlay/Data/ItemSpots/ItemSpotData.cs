using System;
using UnityEngine;

public delegate void OnItemRemovedFromStorageEvent(ItemData item);

[Serializable]
public abstract class ItemSpotData : BaseData
{
    public ItemData ItemInSpot;
    public WorkerData ReservedBy;
    public bool IsEmpty => ItemInSpot == null;
    public bool IsReserved => ReservedBy != null;
    public bool IsEmptyAndAvailable => IsEmpty && !IsReserved;

    public BuildingData Building;

    [SerializeField] Vector2 _localLoc;
    public Vector3 LocalLoc // relative to our Building
    {
        get => _localLoc;
        set
        {
            _localLoc = value;
            UpdateWorldLoc();
        }
    }

    public Vector3 WorldLoc; // relative to the world

    public ItemSpotData(BuildingData buildingData)
    {
        Building = buildingData;
    }

    public virtual void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Building.WorldLoc;
    }

    public void Unreserve()
    {
        Debug.Assert(IsReserved, "Unreserving already unreserved spot (" + InstanceId + ")");
        ReservedBy = null;
    }

    public void AddItem(ItemData item)
    {
        Debug.Assert(ItemInSpot == null, "Adding item when there already is one (" + InstanceId + ")");
        ItemInSpot = item;
    }

    public ItemData RemoveItem()
    {
        var itemToRemove = ItemInSpot;
        ItemInSpot = null;
        return itemToRemove;
    }

    public void ReserveBy(WorkerData worker)
    {
        Debug.Assert(!IsReserved, "Reserving already reserved storage spot (" + InstanceId + ")");
        Debug.Assert(worker != null, "Null reserver (" + InstanceId + ")");

        ReservedBy = worker;
    }
}