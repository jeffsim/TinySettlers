using UnityEngine;

public class Subtask_Noop : Subtask
{
    [SerializeField] public override bool InstantlyComplete { get; set; } = true;
    public Subtask_Noop(Task parentTask) : base(parentTask)
    {
    }

    public override void SubtaskComplete()
    {
    }
}
