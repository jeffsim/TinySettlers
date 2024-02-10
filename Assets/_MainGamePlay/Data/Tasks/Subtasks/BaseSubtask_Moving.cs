using UnityEngine;

public abstract class BaseSubtask_Moving : Subtask
{
    [SerializeField] public LocationComponent Location;
    public override bool IsWalkingToTarget => true;

    public BaseSubtask_Moving(Task parentTask, LocationComponent location) : base(parentTask)
    {
        Location = location;
    }

    public override void Start()
    {
        base.Start();
        Task.LastMoveToTarget.SetWorldLoc(Location);
    }

    public override void Update()
    {
        if (Task.MoveTowards(Task.LastMoveToTarget))
            Task.GotoNextSubstate();
    }
}
