using System;
using UnityEngine;

[Serializable]
public class ItemContainerComponent : BaseData
{
    public override string ToString() => $"{(Item == null ? "empty" : Item)}";

    public ItemData Item;
    public bool IsEmpty => Item == null;
    public bool HasItem => Item != null;

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