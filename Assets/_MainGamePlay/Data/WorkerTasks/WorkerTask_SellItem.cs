using System;
using UnityEngine;

public enum WorkerTask_SellItemSubstate
{
    GotoItemToSell = 0,
    PickupItemToSell = 1,
    SellItem = 2
};

[Serializable]
public class WorkerTask_SellItem : WorkerTask
{
    public override string ToString() => $"Sell item {GetTaskItem()}";
    internal override string getDebuggerString() => $"Sell item {GetTaskItem()}";

    public override TaskType Type => TaskType.SellItem;

    public override ItemDefn GetTaskItem() => spotWithItemToSell.ItemInSpot.Defn;

    [SerializeField] StorageSpotData spotWithItemToSell;

    public const float secondsToPickup = 0.5f;
    public const float secondsToSell = 0.5f;

    public override bool IsWalkingToTarget => substate == 0;

    public override string ToDebugString()
    {
        var str = "Sell item\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_SellItemSubstate.GotoItemToSell: str += "; dist: " + Vector2.Distance(Worker.WorldLoc, spotWithItemToSell.WorldLoc).ToString("0.0"); break;
            case (int)WorkerTask_SellItemSubstate.PickupItemToSell: str += "; per = " + getPercentSubstateDone(secondsToPickup); break;
            case (int)WorkerTask_SellItemSubstate.SellItem: str += "; per = " + getPercentSubstateDone(secondsToSell); break;
            default: Debug.LogError("unknown substate " + substate); break;
        }
        return str;
    }

    // TODO: Pooling
    public static WorkerTask_SellItem Create(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToSell)
    {
        return new WorkerTask_SellItem(worker, needData, spotWithItemToSell);
    }

    private WorkerTask_SellItem(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToSell) : base(worker, needData)
    {
        this.spotWithItemToSell = spotWithItemToSell;
    }

    public override void Start()
    {
        base.Start();

        reserveStorageSpot(spotWithItemToSell);
    }

    public override void OnBuildingDestroyed(BuildingData building)
    {
        // NYI
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        if (substate == (int)WorkerTask_SellItemSubstate.GotoItemToSell)
            LastMoveToTarget += building.WorldLoc - previousWorldLoc;
        else if (spotWithItemToSell.Building == building)
            Worker.WorldLoc += building.WorldLoc - previousWorldLoc;
    }

    public override void OnBuildingPauseToggled(BuildingData building)
    {
        if (building == spotWithItemToSell.Building)
            Abandon();
    }

    public override void Update()
    {
        base.Update();

        switch (substate)
        {
            case (int)WorkerTask_SellItemSubstate.GotoItemToSell:
                if (MoveTowards(spotWithItemToSell.WorldLoc, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_SellItemSubstate.PickupItemToSell:
                if (getPercentSubstateDone(secondsToPickup) == 1)
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_SellItemSubstate.SellItem:
                if (getPercentSubstateDone(secondsToSell) == 1)
                {
                    CompleteTask();
                    Worker.Town.ItemSold(spotWithItemToSell.RemoveItem());
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}