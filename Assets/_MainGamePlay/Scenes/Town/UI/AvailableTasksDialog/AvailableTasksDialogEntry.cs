using System;
using TMPro;
using UnityEngine;

public class AvailableTasksDialogEntry : MonoBehaviour
{
    public TextMeshProUGUI Priority;
    public TextMeshProUGUI Time;
    public TextMeshProUGUI Info;

    [NonSerialized] public PrioritizedTask Task;

    public void ShowForTask(PrioritizedTask task)
    {
        Task = task;
        Update();
    }

    void Update()
    {
        Priority.text = Task.Priority.ToString("0.0");
        Info.text = Task.Task.ToString();
        Time.text = Task.Task.CurSubTask.StartTime.ToString("0.0");
    }
}
