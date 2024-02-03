using System;
using UnityEngine;

public enum WorkerTask_IdleSubstate
{
    ChooseHowLongToWait = 0,
    WaitToGoToNewSpot = 1,
    GoToNewSpot = 2
};

[Serializable]
public class WorkerTask_Idle : WorkerTask
{
    public override string ToString() => "Idle";
    public override TaskType Type => TaskType.Idle;

    [SerializeField] float secondsToWait;
    [SerializeField] LocationComponent idleMoveToDest = new();

    public override bool IsWalkingToTarget => substate == (int)WorkerTask_IdleSubstate.GoToNewSpot;

    public override string ToDebugString()
    {
        var str = "Idle\n";
        str += "  substate: " + substate;
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_Idle Create(WorkerData worker)
    {
        return new(worker);
    }

    private WorkerTask_Idle(WorkerData worker) : base(worker, null)
    {
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        if (building == Worker.AssignedBuilding)
            if (IsWalkingToTarget)
                idleMoveToDest += building.Location - previousLoc;
            else
            {
                // idle our way back to our assigned building's new location
                secondsToWait = 0;
                substate = (int)WorkerTask_IdleSubstate.WaitToGoToNewSpot;
            }
    }

    public override void Update()
    {
        base.Update();
        switch (substate)
        {
            case (int)WorkerTask_IdleSubstate.ChooseHowLongToWait: // choose how long to wait before moving
                secondsToWait = shouldMoveToAssignedBuilding() ? 0 : (1 + UnityEngine.Random.value * 4f);
                GotoNextSubstate();
                break;

            case (int)WorkerTask_IdleSubstate.WaitToGoToNewSpot: // wait to go to a new spot
                if (IsSubstateDone(secondsToWait))
                {
                    idleMoveToDest.SetWorldLoc(Utilities.LocationWithinDistance(Worker.AssignedBuilding.Location, 3f));
                    distanceMovedPerSecond = 3 + (UnityEngine.Random.value - .5f) * 1f;
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_IdleSubstate.GoToNewSpot: // go to new spot
                if (MoveTowards(idleMoveToDest, distanceMovedPerSecond, .1f))
                {
                    // repeat until interrupted
                    Start();
                }
                break;
        }
    }

    private bool shouldMoveToAssignedBuilding()
    {
        return Worker.DistanceToBuilding(Worker.AssignedBuilding) > 5;
    }
}
