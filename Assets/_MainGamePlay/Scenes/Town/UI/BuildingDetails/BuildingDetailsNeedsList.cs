using System.Collections.Generic;
using UnityEngine;

public class BuildingDetailsNeedsList : MonoBehaviour
{
    Building building;

    public GameObject List;
    public BuildingDetailsNeedListEntry BuildingDetailsNeedListEntryPrefab;

    public List<BuildingDetailsNeedListEntry> ListEntries = new();

    public void ShowForBuilding(Building building)
    {
        this.building = building;
        ListEntries.Clear();
        List.RemoveAllChildren();
        Update();
    }

    void Update()
    {
        List.RemoveAllChildren();
        ListEntries.Clear();
        foreach(var need in building.Data.Needs)
        {
            var listEntry = Instantiate(BuildingDetailsNeedListEntryPrefab, List.transform);
            listEntry.ShowForNeed(need);
            ListEntries.Add(listEntry);
        }
        // Update existing needs, add new needs, and remove old needs from ListEntries
        // foreach (var itemInStorage in NeedCounts)
        // {
        //     var listEntry = ListEntries.Find(x => x.ItemDefn == itemInStorage.Key);
        //     if (listEntry == null)
        //     {
        //         // Item doesn't exist in list; add it
        //         // TODO: Sort alphabetically so not jumping around too much
        //         listEntry = Instantiate(BuildingDetailsNeedListEntryPrefab, List.transform);
        //         listEntry.ShowForItem(this, itemInStorage.Key);
        //         ListEntries.Add(listEntry);
        //     }
        //     listEntry.Count.text = itemInStorage.Value.ToString();
        // }

        // //  If ListEntries has an entry that isn't in item-in-storage then it's an item that was removed and we need to remove the list entry
        // for (int i = ListEntries.Count - 1; i >= 0; i--)
        // {
        //     var listEntry = ListEntries[i];
        //     if (!NeedCounts.ContainsKey(listEntry.ItemDefn))
        //     {
        //         ListEntries.RemoveAt(i);
        //         Destroy(listEntry.gameObject);
        //     }
        // }
    }
}
