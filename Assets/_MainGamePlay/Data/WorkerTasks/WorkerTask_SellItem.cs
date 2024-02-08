using System;
using UnityEngine;

[Serializable]
public class WorkerTask_SellItem : WorkerTask
{
    public override string ToString() => $"Sell item {GetTaskItem()}";
    public override TaskType Type => TaskType.SellItem;

    [SerializeField] public IItemSpotInBuilding SpotWithItemToSell;

    public WorkerTask_SellItem(WorkerData worker, NeedData needData, IItemSpotInBuilding spotWithItemToSell) : base(worker, needData)
    {
        SpotWithItemToSell = ReserveSpotOnStart(spotWithItemToSell);
    }

    public override void InitializeStateMachine()
    {
        Subtasks.Add(new WorkerSubtask_WalkToItemSpot(this, SpotWithItemToSell));
        Subtasks.Add(new WorkerSubtask_PickupItemFromBuilding(this, SpotWithItemToSell));
        Subtasks.Add(new WorkerSubtask_SellItemInHands(this, SpotWithItemToSell));
    }
}