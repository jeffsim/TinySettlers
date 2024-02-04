using System;
using UnityEngine;

public interface IAIProvider
{
    AIComponent AI { get; }
}
[Serializable]
public class AIComponent : BaseData
{
    public WorkerTask CurrentTask;
    public bool IsIdle => CurrentTask.Type == TaskType.Idle;
    public WorkerTask_Idle IdleTask;

    public AIComponent(WorkerData worker)
    {
        StartTask(IdleTask = new WorkerTask_Idle(worker));
    }

    internal void StartTask(WorkerTask task)
    {
        CurrentTask = task;
        CurrentTask.Start();
    }

    public void Update() => CurrentTask.Update();
    internal void StartIdling() => StartTask(IdleTask);
}