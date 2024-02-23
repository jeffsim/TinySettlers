using System.Collections.Generic;
using UnityEngine;

public class BuildingDetailsItemList : MonoBehaviour
{
    Building building;

    public GameObject List;
    public ItemsListEntry ItemsListEntryPrefab;

    public List<ItemsListEntry> ListEntries = new();
    public Dictionary<ItemDefn, int> ItemCounts = new();

    public void ShowForBuilding(Building building)
    {
        gameObject.SetActive(building.Data.Defn.CanStoreItems || building.Data.Defn.CanSellGoods);
        if (!gameObject.activeSelf)
            return;
        this.building = building;
        ListEntries.Clear();
        List.RemoveAllChildren();
        Update();
    }

    void Update()
    {
        // First what items we have and aggregate counts
        ItemCounts.Clear();
        foreach (var spot in building.Data.StorageSpots)
            if (spot.Container.FirstItem != null)
            {
                var key = spot.Container.FirstItem.Defn;
                if (ItemCounts.ContainsKey(key))
                    ItemCounts[key]++;
                else
                    ItemCounts[key] = 1;
            }

        // Now that we know what we have:
        //  if an item-in-storage isn't in ListEntries then it's a new item and we need a new list entry
        //  If an item-in-storage is in ListEntries then update its count
        foreach (var itemInStorage in ItemCounts)
        {
            var listEntry = ListEntries.Find(x => x.ItemDefn == itemInStorage.Key);
            if (listEntry == null)
            {
                // Item doesn't exist in list; add it
                // TODO: Sort alphabetically so not jumping around too much
                listEntry = Instantiate(ItemsListEntryPrefab, List.transform);
                listEntry.ShowForItem(this, itemInStorage.Key);
                ListEntries.Add(listEntry);
            }
            listEntry.Count.text = itemInStorage.Value.ToString();
        }

        //  If ListEntries has an entry that isn't in item-in-storage then it's an item that was removed and we need to remove the list entry
        for (int i = ListEntries.Count - 1; i >= 0; i--)
        {
            var listEntry = ListEntries[i];
            if (!ItemCounts.ContainsKey(listEntry.ItemDefn))
            {
                ListEntries.RemoveAt(i);
                Destroy(listEntry.gameObject);
            }
        }
    }
}
