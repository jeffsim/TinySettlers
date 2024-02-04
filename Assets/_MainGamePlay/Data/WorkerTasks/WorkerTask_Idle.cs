using System;

[Serializable]
public class WorkerTask_Idle : WorkerTask
{
    public override string ToString() => "Idle";
    public override TaskType Type => TaskType.Idle;

    public WorkerTask_Idle(WorkerData worker) : base(worker, null)
    {
    }

    public override void InitializeStateMachine()
    {
        var secondsToWait = 1 + UnityEngine.Random.value * 3f;
        if (Worker.Location.DistanceTo(Worker.Assignment.AssignedTo.Location) > 3f) secondsToWait = .1f;
        
        Subtasks.Add(new WorkerSubtask_Wait(this, secondsToWait));

        // Walk to a random location near the assigned building, then wait for a random amount of time
        var loc = Utilities.LocationWithinDistance(Worker.Assignment.AssignedTo.Location, 3f);
        Subtasks.Add(new WorkerSubtask_WalkToLocation(this, loc, Worker.Assignment.AssignedTo));
    }
}
