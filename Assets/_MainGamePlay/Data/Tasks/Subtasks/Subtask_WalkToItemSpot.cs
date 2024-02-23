using UnityEngine;

public class Subtask_WalkToItemSpot : BaseSubtask_Moving
{
    [SerializeField] public IContainerInBuilding ItemSpot;

    public override ItemData GetTaskItem()
    {
        if (ItemSpot.Container.HasItem)
            return ItemSpot.Container.FirstItem;
        if (Task.Worker.Hands.HasItem)
            return Task.Worker.Hands.FirstItem;
        return null;
    }

    public Subtask_WalkToItemSpot(Task parentTask, IContainerInBuilding itemSpot) : base(parentTask, itemSpot.Location)
    {
        ItemSpot = itemSpot;
        UpdateMoveTargetWhenBuildingMoves(ItemSpot.Building);
    }
}
