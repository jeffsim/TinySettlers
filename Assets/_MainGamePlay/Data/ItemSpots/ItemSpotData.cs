using System;
using UnityEngine;

public delegate void OnItemRemovedFromStorageEvent(ItemData item);

[Serializable]
public abstract class ItemSpotData : ReservableData
{
    public override string ToString() => $"{(ItemInSpot == null ? "empty" : ItemInSpot)}";

    public ItemData ItemInSpot;
    public bool IsEmpty => ItemInSpot == null;
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

    internal void OnBuildingDestroyed()
    {
        if (!IsEmpty)
        {
            Building.Town.AddItemToGround(ItemInSpot, WorldLoc);
            RemoveItem();
        }
    }
}