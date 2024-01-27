// Not serialized - just used internally by BuildingData
using System;

public class PrioritizedTask
{
    public override string ToString() => $"{Priority:F1} {Task.getDebuggerString()}";

    internal void Set(WorkerTask task, float priority)
    {
        Task = task; Priority = priority;
    }

    public WorkerTask Task;
    public float Priority;

    public PrioritizedTask(WorkerTask task = null, float priority = 0)
    {
        Task = task; Priority = priority;
    }
}
