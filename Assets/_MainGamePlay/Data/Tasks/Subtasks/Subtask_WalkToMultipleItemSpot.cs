using UnityEngine;

public class Subtask_WalkToMultipleItemSpot : BaseSubtask_Moving
{
    [SerializeField] public IContainerInBuilding ItemsSpot;

    public override ItemData GetTaskItem()
    {
        if (ItemsSpot.Container.HasItem)
            return ItemsSpot.Container.FirstItem;
        if (Task.Worker.Hands.HasItem)
            return Task.Worker.Hands.FirstItem;
        return null;
    }

    public Subtask_WalkToMultipleItemSpot(Task parentTask, IContainerInBuilding itemsSpot) : base(parentTask, itemsSpot.Location)
    {
        ItemsSpot = itemsSpot;
        UpdateMoveTargetWhenBuildingMoves(ItemsSpot.Building);
    }
}
