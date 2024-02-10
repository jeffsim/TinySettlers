using UnityEngine;

public class Subtask_UnreserveSpot : Subtask
{
    [SerializeField] public IItemSpotInBuilding ItemSpot;
    [SerializeField] public override bool InstantlyRun { get; set; } = true;

    public Subtask_UnreserveSpot(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
    }

    public override void SubtaskComplete()
    {
        Task.UnreserveSpot(ItemSpot);
    }
}
