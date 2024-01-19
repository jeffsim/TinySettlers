using System;
using UnityEngine;

public delegate void OnItemRemovedFromStorageEvent(ItemData item);

[Serializable]
public class StorageSpotData : BaseData
{
    public ItemData ItemInStorage;
    public WorkerData ReservedBy;
    public bool IsEmpty => ItemInStorage == null;
    public bool IsReserved => ReservedBy != null;
    public bool IsEmptyAndAvailable => IsEmpty && !IsReserved;

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

    public Vector3 WorldLoc;
    [SerializeField] StorageAreaData Area;

    public BuildingData Building => Area.Building;

    public StorageSpotData(StorageAreaData area, int index)
    {
        Area = area;
        LocalLoc = new Vector2((index % Building.Defn.StorageAreaWidthAndHeight) * 1.1f - 1.1f, -(index / Building.Defn.StorageAreaWidthAndHeight) * 1.1f + 1.1f);
    }

    public void UpdateWorldLoc()
    {
        WorldLoc = LocalLoc + Area.WorldLoc;
    }

    public void Unreserve()
    {
        Debug.Assert(IsReserved, "Unreserving already unreserved spot (" + InstanceId + ")");
        ReservedBy = null;
    }

    public void AddItem(ItemData item)
    {
        Debug.Assert(ItemInStorage == null, "Adding item when there already is one (" + InstanceId + ")");
        ItemInStorage = item;
    }

    public void RemoveItem()
    {
        Debug.Assert(!IsReserved, "shouldn't be reserved when item removed (" + InstanceId + ")");
        ItemInStorage = null;
    }

    public void ReserveBy(WorkerData worker)
    {
        Debug.Assert(!IsReserved, "Reserving already reserved storage spot (" + InstanceId + ")");
        Debug.Assert(worker != null, "Null reserver (" + InstanceId + ")");

        ReservedBy = worker;
    }

    // public bool CanStoreItem(ItemDefn itemDefn, int fuckinMagic = 0)
    // {
    //     // can only store items of the same type in any one cell
    //     if (ItemInStorage != null)
    //         if (itemDefn != null && ItemInStorage.DefnId != itemDefn.Id)
    //             return false;

    //     // DO count reserved spots for this function
    //     return StoredItems.Count + NumReservedSpots + fuckinMagic < NumOfItemTypeThatCanBeStored(itemDefn);
    // }

}