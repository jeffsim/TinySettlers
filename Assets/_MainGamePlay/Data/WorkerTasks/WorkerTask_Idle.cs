using System;
using UnityEngine;

[Serializable]
public class WorkerTask_Idle : WorkerTask
{
    public override string ToString() => "Idle";
    
    public override TaskType Type => TaskType.Idle;

    [SerializeField] float secondsToWait;
    [SerializeField] Vector3 idleMoveToDest;

    public override bool Debug_IsMovingToTarget
    {
        get
        {
            return substate == 2;
        }
    }

    public override string ToDebugString()
    {
        var str = "Idle\n";
        str += "  substate: " + substate;
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_Idle Create(WorkerData worker)
    {
        var task = new WorkerTask_Idle(worker);
        return task;
    }

    private WorkerTask_Idle(WorkerData worker) : base(worker)
    {
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        if (building == Worker.AssignedBuilding)
            if (substate == 2)
                idleMoveToDest += building.WorldLoc - previousWorldLoc;
            else
            {
                // idle our way back to our assigned building's new location
                secondsToWait = 0;
                substate = 1;
            }
    }

    public override void Update()
    {
        base.Update();
        switch (substate)
        {
            case 0: // choose how long to wait before moving
                secondsToWait = shouldMoveToAssignedBuilding() ? 0 : (1 + UnityEngine.Random.value * 4f);
                gotoNextSubstate();
                break;

            case 1: // wait to go to a new spot
                if (getPercentSubstateDone(secondsToWait) == 1)
                {
                    idleMoveToDest = Utilities.locationWithinDistance(Worker.AssignedBuilding.WorldLoc, 3f);
                    distanceMovedPerSecond = 3 + (UnityEngine.Random.value - .5f) * 1f;
                    gotoNextSubstate();
                }
                break;

            case 2: // go to new spot
                if (moveTowards(idleMoveToDest, distanceMovedPerSecond, .1f))
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
