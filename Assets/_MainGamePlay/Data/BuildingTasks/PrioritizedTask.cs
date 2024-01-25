using System;
using System.Collections.Generic;
using UnityEngine;

// Not serialized - just used internally by BuildingData
public class PrioritizedTask
{
    public override string ToString() => $"{Priority:F1} {Task.getDebuggerString()}";

    public WorkerTask Task;
    public float Priority;

    public PrioritizedTask(WorkerTask task, float priority)
    {
        Task = task; Priority = priority;
    }
}
