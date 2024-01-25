using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum SortNeedsDisplayBy { Building, Priority, TaskType };

public class AllNeedsDetails : MonoBehaviour
{
    public TextMeshProUGUI Needs;
    SceneWithMap scene;
    public SortNeedsDisplayBy SortBy = SortNeedsDisplayBy.Building;
    public GameObject List;
    public NeedDetailsEntry NeedDetailsEntryPrefab;
    private Queue<NeedDetailsEntry> entryPool = new Queue<NeedDetailsEntry>();

    public void Show(SceneWithMap scene, SortNeedsDisplayBy sortBy)
    {
        SortBy = sortBy;
        gameObject.SetActive(true);
        this.scene = scene;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (scene == null) return;

        var town = scene.Map.Town;
        ResetEntryPool();
        List<NeedData> needs = new();
        foreach (var building in town.Buildings)
            needs.AddRange(building.Needs);
        needs.AddRange(town.otherTownNeeds);
        switch (SortBy)
        {
            case SortNeedsDisplayBy.Building: break;
            case SortNeedsDisplayBy.Priority: needs.Sort((a, b) => (int)((b.Priority - a.Priority) * 1000)); break;
            case SortNeedsDisplayBy.TaskType: needs.Sort((a, b) => (int)((b.Type - a.Type) * 1000)); break;
        }
        foreach (var need in needs)
        {
            var entry = GetOrCreateEntry();
            entry.ShowForNeed(need);
            entry.gameObject.SetActive(true);
        }
    }

    private NeedDetailsEntry GetOrCreateEntry()
    {
        if (entryPool.Count > 0)
        {
            return entryPool.Dequeue();
        }
        else
        {
            var newEntry = Instantiate(NeedDetailsEntryPrefab, List.transform);
            return newEntry;
        }
    }

    private void ResetEntryPool()
    {
        foreach (Transform child in List.transform)
        {
            var entry = child.GetComponent<NeedDetailsEntry>();
            if (entry != null)
            {
                entry.gameObject.SetActive(false);
                entryPool.Enqueue(entry);
            }
        }
    }
}
