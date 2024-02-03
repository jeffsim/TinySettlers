using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MultipleItemContainerComponent : BaseData
{
    public override string ToString() => Items.Count == 0 ? "empty" : "{" + string.Join(", ", Items.Select(item => item)) + "}";

    public List<ItemData> Items = new();

    public bool IsEmpty => Items.Count == 0;
    public bool HasItem => Items.Count > 0;

    public void AddItem(ItemData item)
    {
        Debug.Assert(item != null, "Adding null item to " + this);
        Debug.Assert(!Items.Contains(item), item + " already in " + this);
        Items.Add(item);
    }

    public void ClearItems() => Items.Clear();
}