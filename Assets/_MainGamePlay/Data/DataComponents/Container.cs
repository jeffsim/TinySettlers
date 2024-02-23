using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IContainerInBuilding : IReservable
{
    Container Container { get; }
    Location Location { get; set; }
    BuildingData Building { get; set; }
}

[Serializable]
public class Container : BaseData
{
    public override string ToString() => Items.Count == 0 ? "empty" : "{" + string.Join(", ", Items.Select(item => item)) + "}";

    public List<ItemData> Items = new();

    public bool IsEmpty => Items.Count == 0;
    public bool HasItem => Items.Count > 0;
    public ItemData FirstItem => HasItem ? Items[0] : null;

    internal bool ContainsItem(ItemDefn itemDefn)
    {
        return Items.Any(item => item.Defn.Id == itemDefn.Id);
    }

    public void AddItem(ItemData item)
    {
        Debug.Assert(item != null, "Adding null item to " + this);
        Debug.Assert(!Items.Contains(item), item + " already in " + this);
        Items.Add(item);
    }

    public void RemoveItem(ItemData item)
    {
        Debug.Assert(item != null, "Removing null item from " + this);
        Debug.Assert(Items.Contains(item), item + " not in " + this);
        Items.Remove(item);
    }

    public ItemData ClearItems()
    {
        var firstItem = FirstItem;
        Items.Clear();
        return firstItem;
    }
}