using System;

[Serializable]
public class Task_Idle : Task
{
    public override string ToString() => "Idle";
    public override TaskType Type => TaskType.Idle;

    public Task_Idle(WorkerData worker) : base(worker, null)
    {
    }

    public override void InitializeStateMachine()
    {
        float distanceToAssignedBuilding = Worker.Location.DistanceTo(Worker.Assignment.AssignedTo.Location);
        var secondsToWait = (distanceToAssignedBuilding > 3f) ? .1f : 1 + UnityEngine.Random.value * 3f;
        Subtasks.Add(new Subtask_Wait(this, secondsToWait));

        // Walk to a random location near the assigned building, then wait for a random amount of time
        var loc = Utilities.LocationWithinDistance(Worker.Assignment.AssignedTo.Location, 3f);
        Subtasks.Add(new Subtask_WalkToLocation(this, loc, Worker.Assignment.AssignedTo));
    }
}
