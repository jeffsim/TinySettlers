using UnityEngine;

public class Subtask_Wait : Subtask
{
    [SerializeReference] Location Location;
    public override bool AutomaticallyAbandonIfAssignedBuildingPaused { get; set; } = false;
    public override bool AutomaticallyAbandonIfAssignedBuildingDestroyed { get; set; } = false;
    public override bool AutomaticallyAbandonIfAssignedBuildingMoved { get; set; } = true;

    public Subtask_Wait(Task parentTask, float secondsToWait) : base(parentTask)
    {
        RunTime = secondsToWait;
    }
}
