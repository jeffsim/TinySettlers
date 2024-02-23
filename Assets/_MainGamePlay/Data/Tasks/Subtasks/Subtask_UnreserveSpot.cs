using UnityEngine;

public class Subtask_UnreserveSpot : Subtask
{
    [SerializeField] public override bool InstantlyComplete { get; set; } = true;
    [SerializeField] public IContainerInBuilding ItemSpot;

    public Subtask_UnreserveSpot(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
    }

    public override void SubtaskComplete()
    {
        Task.UnreserveSpot(ItemSpot);
    }
}
