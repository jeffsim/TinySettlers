using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AvailableTasksDialog : MonoBehaviour
{
    public TextMeshProUGUI Needs;
    SceneWithMap scene;
    public GameObject List;
    public AvailableTasksDialogEntry AvailableTasksDialogEntryPrefab;

    public void Show(SceneWithMap scene)
    {
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
        if (town.TownTaskMgr.LastSeenPrioritizedTasks == null)
            return;
            
        List.RemoveAllChildren();

        foreach (var task in town.TownTaskMgr.LastSeenPrioritizedTasks)
        {
          //  if (!task.Task.IsRunning) continue;
            var entry = Instantiate(AvailableTasksDialogEntryPrefab, List.transform);
            entry.ShowForTask(task);
        }
    }
}
