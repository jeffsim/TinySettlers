using System;
using UnityEngine;

public enum WorkerTask_PickupGatherableResourceSubstate
{
    GotoGatheringSpot = 0,
    ReapGatherableResource = 1,
    PickupGatherableResource = 2,
};

[Serializable]
public class WorkerTask_PickupGatherableResource : WorkerTask
{
    public override string ToString() => "Pickup gatherable resource";
    internal override string getDebuggerString() => $"Pickup gatherable resource from {optimalGatheringSpot}";

    public override TaskType Type => TaskType.PickupGatherableResource;

    [SerializeField] GatheringSpotData optimalGatheringSpot;
    [SerializeField] StorageSpotData reservedStorageSpot;

#if UNITY_INCLUDE_TESTS
    public GatheringSpotData OptimalGatheringSpot => optimalGatheringSpot;
    public StorageSpotData ReservedStorageSpot => reservedStorageSpot;
#endif

    public const float secondsToReap = 1;
    public const float secondsToPickup = 0.5f;

    public override bool IsWalkingToTarget => substate == 0;

    public override ItemDefn GetTaskItem()
    {
        if (optimalGatheringSpot == null || optimalGatheringSpot == null) return null;
        return optimalGatheringSpot.ItemInSpot.Defn;
    }

    public override string ToDebugString()
    {
        var str = "Pickup gatherable resource\n";
        str += "  Gather from: " + optimalGatheringSpot + " (" + optimalGatheringSpot.Building + "), gatherspot: " + optimalGatheringSpot.InstanceId + "\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, optimalGatheringSpot.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource: str += "; per = " + getPercentSubstateDone(secondsToReap); break;
            case (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_PickupGatherableResource Create(WorkerData worker, NeedData needData, GatheringSpotData optimalGatheringSpot, StorageSpotData storageSpotToReserve)
    {
        return new WorkerTask_PickupGatherableResource(worker, needData, optimalGatheringSpot, storageSpotToReserve);
    }

    private WorkerTask_PickupGatherableResource(WorkerData worker, NeedData needData, GatheringSpotData optimalGatheringSpot, StorageSpotData storageSpotToReserve) : base(worker, needData)
    {
        this.optimalGatheringSpot = optimalGatheringSpot;

        // While this task is simply to go pick up a gatherable resource, we wouldn't start the task if we didn't know that there was at least one place that we could bring the
        // resource to; we reserve that so that if no building needs it after we pick it up, we can still store it somewhere
        reservedStorageSpot = storageSpotToReserve;
    }

    public override void Start()
    {
        base.Start();

        // Now that we've actually started the task, we can reserve the already-determined-to-be optimal gathering spot that was passed in above.
        reserveGatheringSpot(optimalGatheringSpot);
        reserveStorageSpot(reservedStorageSpot);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // If our target resource-gathering building was destroyed and we're still walking to it, or gathering from it, then abandon
        if (optimalGatheringSpot.Building.IsDestroyed && substate == 0)
            Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        // If we're moving towards the building that was moved, then update our movement target
        // If we're working in the building that was moved, then update our location
        bool updateMoveToLoc = false, updateWorkerLoc = false;
        switch ((WorkerTask_PickupGatherableResourceSubstate)substate)
        {
            case WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot: updateMoveToLoc = building == optimalGatheringSpot.Building; break;
            case WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource: updateWorkerLoc = building == optimalGatheringSpot.Building; break;
            case WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource: updateWorkerLoc = building == optimalGatheringSpot.Building; break;
        }
        if (updateMoveToLoc) LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        if (updateWorkerLoc) Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot: // go to resource spot
                if (MoveTowards(optimalGatheringSpot.WorldLoc, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource: // reap item (e.g. cut down tree)
                if (getPercentSubstateDone(secondsToReap) == 1)
                {
                    // Done reaping.
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource: // gather in the building.
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    // Remove item from gathering spot and put it in Worker's hand, and we're done
                    CompleteTask();
                    Worker.AddItemToHands(optimalGatheringSpot.RemoveItem());

                    // NOTE that completing the task unreserved both the gathering spot and the storage spot so that others can use them.
                    // However, we don't actually want to unreserve the storage spot yet since the worker is now holding the item and may need
                    // to store in that spot if no building needs it.  So: re-reserve it (ick).  I don't want to combine pickup and deliver tasks into one
                    // for the reasons that I broke them apart in the first place...
                    Worker.StorageSpotReservedForItemInHand = reservedStorageSpot;
                    Worker.OriginalPickupItemNeed = Need;
                    reservedStorageSpot.ReserveBy(Worker);
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}