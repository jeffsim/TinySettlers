public class Subtask_WalkToLocation : BaseSubtask_Moving
{
    public Subtask_WalkToLocation(Task parentTask, Location location, BuildingData building = null) : base(parentTask, location)
    {
        if (building != null)
            UpdateMoveTargetWhenBuildingMoves(building);
    }
}
