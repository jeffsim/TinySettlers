using UnityEngine;

public class WorkerSubtask_WalkToLocation : BaseWorkerSubtask_Moving
{
    public WorkerSubtask_WalkToLocation(WorkerTask parentTask, LocationComponent location, BuildingData building = null) : base(parentTask, location)
    {
        if (building != null)
            UpdateMoveTargetWhenBuildingMoves(building);
    }
}
