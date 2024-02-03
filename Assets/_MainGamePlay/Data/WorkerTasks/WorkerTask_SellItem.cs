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
    internal override string GetDebuggerString() => $"Sell item {GetTaskItem()}";

    public override TaskType Type => TaskType.SellItem;

    public override ItemDefn GetTaskItem()
    {
        if (spotWithItemToSell.ItemContainer.Item != null)
            return spotWithItemToSell.ItemContainer.Item.Defn;
        if (Worker.Hands.HasItem)
            return Worker.Hands.Item.Defn;
        return null;
    }

    [SerializeField] StorageSpotData spotWithItemToSell;

    public const float secondsToPickup = 0.5f;
    public const float secondsToSell = 2f;

    public override bool IsWalkingToTarget => substate == (int)WorkerTask_SellItemSubstate.GotoItemToSell;

    // TODO: Pooling
    public static WorkerTask_SellItem Create(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToSell)
    {
        return new(worker, needData, spotWithItemToSell);
    }

    private WorkerTask_SellItem(WorkerData worker, NeedData needData, StorageSpotData spotWithItemToSell) : base(worker, needData)
    {
        this.spotWithItemToSell = ReserveSpotOnStart(spotWithItemToSell);
    }

    // Note: this is called when any building is destroyed, not just "this task's" building
    public override void OnBuildingDestroyed(BuildingData building)
    {
        if (building != Worker.AssignedBuilding) return; // This task only cares if our building was the one that was destroyed

        if (substate == (int)WorkerTask_SellItemSubstate.SellItem)
        {
            // We've picked up the item and are trying to sell it; need to find a destination to bring it to, or drop it on the ground
            var newSpot = FindNewOptimalStorageSpotToDeliverItemTo(spotWithItemToSell, Worker.Location);
            if (newSpot == spotWithItemToSell)
            {
                // Failed to find an alternative; drop the item on the ground for later handling when storage becomes available
                Worker.Town.AddItemToGround(Worker.Hands.ClearItem(), Worker.Location);
            }
            else
            {
                // We found an alternative spot; the cleanest thing here would be to simply drop the item anyways, but then the 
                // worker will drop and then on next update pick it back up.  Instead, what we'll do is fake our way into a state
                // where we continue to hold onto to the item, but are ready to instantly start carrying it to the new spot.
                ReservedSpots.Remove(spotWithItemToSell);
                Worker.StorageSpotReservedForItemInHand = newSpot;
                Worker.OriginalPickupItemNeed = NeedData.CreateAbandonedItemCleanupNeed(Worker.Hands.Item);
            }
            Abandon();
        }
    }

    public override void OnBuildingMoved(BuildingData building, LocationComponent previousLoc)
    {
        if (building != spotWithItemToSell.Building) return;
        if (IsWalkingToTarget)
            LastMoveToTarget += building.Location - previousLoc;
        else
            Worker.Location += building.Location - previousLoc;
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
                if (MoveTowards(spotWithItemToSell.Location))
                    GotoNextSubstate();
                break;

            case (int)WorkerTask_SellItemSubstate.PickupItemToSell:
                if (IsSubstateDone(secondsToPickup))
                {
                    Worker.Hands.SetItem(spotWithItemToSell.ItemContainer.ClearItem());
                    Worker.StorageSpotReservedForItemInHand = null; // TODO
                    GotoNextSubstate();
                }
                break;

            case (int)WorkerTask_SellItemSubstate.SellItem:
                if (IsSubstateDone(secondsToSell))
                {
                    var item = Worker.Hands.ClearItem();
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