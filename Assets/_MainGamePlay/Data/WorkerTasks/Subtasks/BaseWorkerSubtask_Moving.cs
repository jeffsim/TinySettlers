using UnityEngine;

public abstract class BaseWorkerSubtask_Moving : WorkerSubtask
{
    [SerializeField] LocationComponent Location;
    public override bool IsWalkingToTarget => true;

    public BaseWorkerSubtask_Moving(WorkerTask parentTask, LocationComponent location) : base(parentTask)
    {
        Location = location;
    }

    public override void Start()
    {
        base.Start();
        Task.LastMoveToTarget = Location;
    }

    public override void Update()
    {
        if (Task.MoveTowards(Task.LastMoveToTarget))
            Task.GotoNextSubstate();
    }
}
