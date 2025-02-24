using System;
using UnityEngine;

[Serializable]
public class Task_SellItem : Task
{
    public override string ToString() => $"Sell item {GetTaskItem()}";
    public override TaskType Type => TaskType.SellItem;

    [SerializeField] public IContainerInBuilding SpotWithItemToSell;

    public Task_SellItem(WorkerData worker, NeedData needData, IContainerInBuilding spotWithItemToSell) : base(worker, needData)
    {
        SpotWithItemToSell = ReserveSpotOnStart(spotWithItemToSell);
    }

    public override Subtask GetNextSubtask()
    {
        return SubtaskIndex switch
        {
            0 => new Subtask_WalkToItemSpot(this, SpotWithItemToSell),
            1 => new Subtask_PickupItemFromItemSpot(this, SpotWithItemToSell),
            2 => new Subtask_SellItemInHands(this, SpotWithItemToSell),
            _ => null // No more subtasks
        };
    }
}