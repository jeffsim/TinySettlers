using System;
using UnityEngine;

[Serializable]
public class Task_SellItem : Task
{
    public override string ToString() => $"Sell item {GetTaskItem()}";
    public override TaskType Type => TaskType.SellItem;

    [SerializeField] public IItemSpotInBuilding SpotWithItemToSell;

    public Task_SellItem(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToSell) : base(worker, needData)
    {
        SpotWithItemToSell = ReserveSpotOnStart(spotWithItemToSell);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new Subtask_WalkToItemSpot(this, SpotWithItemToSell));
        Subtasks.Add(new Subtask_PickupItemFromItemSpot(this, SpotWithItemToSell));
        Subtasks.Add(new Subtask_SellItemInHands(this, SpotWithItemToSell));
    }
}