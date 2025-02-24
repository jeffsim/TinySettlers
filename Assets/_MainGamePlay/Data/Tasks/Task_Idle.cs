using System;

[Serializable]
public class Task_Idle : Task
{
    public override string ToString() => "Idle";
    public override TaskType Type => TaskType.Idle;

    public Task_Idle(WorkerData worker) : base(worker, null)
    {
    }

    public override Subtask GetNextSubtask()
    {
        switch (SubtaskIndex)
        {
            case 0:
                float distanceToAssignedBuilding = Worker.Location.DistanceTo(Worker.Assignable.AssignedTo.Location);
                var secondsToWait = (distanceToAssignedBuilding > 3f) ? .1f : 1 + UnityEngine.Random.value * 3f;
                return new Subtask_Wait(this, secondsToWait);
            case 1:
                // Walk to a random location near the assigned building, then wait for a random amount of time
                var loc = Utilities.LocationWithinDistance(Worker.Assignable.AssignedTo.Location, 3f);
                return new Subtask_WalkToLocation(this, loc, Worker.Assignable.AssignedTo);
            default:
                return null; // done
        }
    }
}
