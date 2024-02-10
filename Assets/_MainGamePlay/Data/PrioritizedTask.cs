// Not serialized - just used internally by BuildingData
using System;

public class PrioritizedTask
{
    public override string ToString() => $"{Priority:F1} {Task}";

    internal void Set(Task task, float priority)
    {
        Task = task; Priority = priority;
    }

    public Task Task;
    public float Priority;

    public PrioritizedTask(Task task = null, float priority = 0)
    {
        Task = task; Priority = priority;
    }
}
