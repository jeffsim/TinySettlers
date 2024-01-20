using System;
using TMPro;
using UnityEngine;

public class ItemsListEntry : MonoBehaviour
{
    public TextMeshProUGUI Type;
    public TextMeshProUGUI Count;
    [NonSerialized] public ItemDefn ItemDefn;
    BuildingDetailsItemList list;
    public void ShowForItem(BuildingDetailsItemList list, ItemDefn itemDefn)
    {
        this.list = list;
        ItemDefn = itemDefn;
        Type.text = itemDefn.FriendlyName;
        Update();
    }

    void Update()
    {
        // Get count of items in our building which match our item defn
        Count.text = list.ItemCounts[ItemDefn].ToString();
    }
}
