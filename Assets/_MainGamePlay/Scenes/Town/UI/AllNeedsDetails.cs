using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum SortNeedsDisplayBy { Building, Priority, TaskType };
public class AllNeedsDetails : MonoBehaviour
{
    public TextMeshProUGUI Needs;
    SceneWithMap scene;
    public SortNeedsDisplayBy SortBy = SortNeedsDisplayBy.Building;

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

        var str = "";
        switch (SortBy)
        {
            case SortNeedsDisplayBy.Building:
                foreach (var building in scene.Map.Town.Buildings)
                {
                    if (building.Needs.Count == 0) continue;
                    str += "<color=yellow>= " + building.DefnId + " ====</color> \n";
                    // sort by priority within the building
                    var needs = new List<NeedData>(building.Needs);
                    str += Utilities.getNeedsDebugString(needs, false);
                    str += "\n";
                }

                // add town otherneeds
                str += "<color=yellow>= Town ====</color> \n";
                foreach (var need in scene.Map.Town.otherTownNeeds)
                {
                    str += need.Type + ": " + need.Priority + " - " + need.State + "\n";
                }
                break;

            case SortNeedsDisplayBy.TaskType:
                var needsByType = new List<NeedData>();
                foreach (var building in scene.Map.Town.Buildings)
                    needsByType.AddRange(building.Needs);
                needsByType.AddRange(scene.Map.Town.otherTownNeeds);
                needsByType.Sort((a, b) => (int)((b.Type - a.Type) * 1000));
                str += Utilities.getNeedsDebugString(needsByType, true);
                break;

            case SortNeedsDisplayBy.Priority:
                // TODO (PERF): Keep list of all needs
                var allNeeds = new List<NeedData>();
                foreach (var building in scene.Map.Town.Buildings)
                    allNeeds.AddRange(building.Needs);
                allNeeds.AddRange(scene.Map.Town.otherTownNeeds);
                str += Utilities.getNeedsDebugString(allNeeds, true);
                break;
        }

        Needs.text = str;
    }
}
