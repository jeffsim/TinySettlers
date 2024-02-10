using System;
using UnityEngine;

public interface IAIProvider
{
    AIComponent AI { get; }
}
[Serializable]
public class AIComponent : BaseData
{
    public override string ToString() => $"Task: {CurrentTask} - {CurrentTask.CurSubTask}";

    public Task CurrentTask;
    public bool IsIdle => CurrentTask.Type == TaskType.Idle;
    public Task_Idle IdleTask;

    public AIComponent(WorkerData worker)
    {
        StartTask(IdleTask = new Task_Idle(worker));
    }

    internal void StartTask(Task task)
    {
        CurrentTask = task;
        CurrentTask.Start();
    }

    public void Update() => CurrentTask.Update();
    internal void StartIdling() => StartTask(IdleTask);
}