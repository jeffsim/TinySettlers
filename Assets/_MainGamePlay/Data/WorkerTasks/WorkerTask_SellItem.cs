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

    public override ItemDefn GetTaskItem() => spotWithItemToSell.ItemContainer.Item != null ? spotWithItemToSell.ItemContainer.Item.Defn : Worker.ItemInHand.Defn;

    [SerializeField] StorageSpotData spotWithItemToSell;

    public const float secondsToPickup = 0.5f;
    public const float secondsToSell = 2f;

    public override bool IsWalkingToTarget => substate == (int)WorkerTask_SellItemSubstate.GotoItemToSell;

    public override string ToDebugString()
    {
        var str = "Sell item\n";
        str += "  substate: " + substate;
        switch (substate)
        {
            case (int)WorkerTask_SellItemSubstate.GotoItemToSell: str += "; dist: " + Worker.Location.DistanceTo(spotWithItemToSell.Location).ToString("0.0"); break;
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
        if (building != spotWithItemToSell.Building) return;

        if (substate == (int)WorkerTask_SellItemSubstate.SellItem)
        {
            // We've picked up the item and are trying to sell it; need to find a destination to bring it to, or drop it on the ground
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(spotWithItemToSell, Worker.Location);
            if (newSpot == spotWithItemToSell)
            {
                // Failed to find an alternative; drop the item on the ground for later handling when storage becomes available
                Worker.Town.AddItemToGround(Worker.RemoveItemFromHands(), Worker.Location);
            }
            else
            {
                // We found an alternative spot; the cleanest thing here would be to simply drop the item anyways, but then the 
                // worker will drop and then on next update pick it back up.  Instead, what we'll do is fake our way into a state
                // where we continue to hold onto to the item, but are ready to instantly start carrying it to the new spot.
                ReservedStorageSpots.Remove(spotWithItemToSell);
                Worker.StorageSpotReservedForItemInHand = newSpot;
                Worker.OriginalPickupItemNeed = NeedData.CreateAbandonedItemCleanupNeed(Worker.ItemInHand);
            }
        }
        Abandon();
    }

    public override void OnBuildingMoved(BuildingData building, Vector3 previousWorldLoc)
    {
        if (building != spotWithItemToSell.Building) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location.WorldLoc - previousWorldLoc;
        else
            Worker.Location.WorldLoc += building.Location.WorldLoc - previousWorldLoc;
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
                if (MoveTowards(spotWithItemToSell.Location.WorldLoc, distanceMovedPerSecond))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_SellItemSubstate.PickupItemToSell:
                if (getPercentSubstateDone(secondsToPickup) == 1)
                {
                    Worker.AddItemToHands(spotWithItemToSell.ItemContainer.ClearItem());
                    Worker.StorageSpotReservedForItemInHand = null; // TODO
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_SellItemSubstate.SellItem:
                if (getPercentSubstateDone(secondsToSell) == 1)
                {
                    var item = Worker.RemoveItemFromHands();
                    Worker.Town.ItemSold(item);
                    CompleteTask();
                }
                break;

            default:
                Debug.LogError("unknown substate " + substate);
                break;
        }
    }
}