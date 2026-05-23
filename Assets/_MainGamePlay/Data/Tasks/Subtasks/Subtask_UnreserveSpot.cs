using UnityEngine;

public class Subtask_UnreserveSpot : Subtask
{
    public override bool InstantlyComplete { get; set; } = true;
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public Subtask_UnreserveSpot(Task parentTask, IItemSpotInBuilding itemSpot) : base(parentTask)
    {
        ItemSpot = itemSpot;
    }

    public override void SubtaskComplete()
    {
        Task.UnreserveSpot(ItemSpot);
    }
}
