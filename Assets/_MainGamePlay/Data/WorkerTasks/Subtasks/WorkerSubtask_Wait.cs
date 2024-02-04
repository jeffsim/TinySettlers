using Sirenix.OdinInspector;
using UnityEngine;

public class WorkerSubtask_Wait : WorkerSubtask
{
    [SerializeReference] LocationComponent Location;
    public override bool AutomaticallyAbandonIfAssignedBuildingPaused { get; set; } = false;
    public override bool AutomaticallyAbandonIfAssignedBuildingDestroyed { get; set; } = false;
    public override bool AutomaticallyAbandonIfAssignedBuildingMoved { get; set; } = true;

    public WorkerSubtask_Wait(WorkerTask parentTask, float secondsToWait) : base(parentTask)
    {
        RunTime = secondsToWait;
    }
}
