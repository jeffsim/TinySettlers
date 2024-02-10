using UnityEngine;

public class Subtask_UnreserveSpot : Subtask
{
    [SerializeField] public override bool InstantlyRun { get; set; } = true;

    public Subtask_UnreserveSpot(Task parentTask) : base(parentTask)
    {
    }

    public override void SubtaskComplete()
    {
        Task.UnreserveSpot(ItemSpot);
    }
}
