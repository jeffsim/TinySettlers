using UnityEngine;

public abstract class BaseWorkerTask_TransportItemFromSpotToStorage : WorkerTask
{
    [SerializeField] public IItemSpotInBuilding SpotWithItemToPickup;
    [SerializeField] public IItemSpotInBuilding ReservedSpotToStoreItemIn;

    public BaseWorkerTask_TransportItemFromSpotToStorage(WorkerData worker, NeedData need, IItemSpotInBuilding spotWithItemToPickup, IItemSpotInBuilding reservedSpotToStoreItemIn) : base(worker, need)
    {
        SpotWithItemToPickup = ReserveSpotOnStart(spotWithItemToPickup);
        ReservedSpotToStoreItemIn = ReserveSpotOnStart(reservedSpotToStoreItemIn);
    }

    public override void OnBuildingDestroyed(BuildingData building) => handleBuildingPausedOrDestroyed(building, true);
    public override void OnBuildingPauseToggled(BuildingData building) => handleBuildingPausedOrDestroyed(building, false);

    void handleBuildingPausedOrDestroyed(BuildingData building, bool destroyed)
    {
        if (building == ReservedSpotToStoreItemIn.Building)
        {
            ReservedSpots.Remove(ReservedSpotToStoreItemIn);
            ReservedSpotToStoreItemIn = FindAndReserveNewOptimalStorageSpotToDeliverItemTo(ReservedSpotToStoreItemIn, SpotWithItemToPickup.Location);
            if (ReservedSpotToStoreItemIn == null)
            {
                Abandon(); // failed to find a new spot to store the item in
                return;
            } 
            ReservedSpots.Add(ReservedSpotToStoreItemIn);
        }
        if (destroyed)
            base.OnBuildingDestroyed(building);
        else
            base.OnBuildingPauseToggled(building);
    }

    public override void AllSubtasksComplete()
    {
        CompleteTask();
        Worker.StorageSpotReservedForItemInHand = ReservedSpotToStoreItemIn;
        Worker.OriginalPickupItemNeed = Need;
        ReservedSpotToStoreItemIn.Reservation.ReserveBy(Worker);
    }
}
