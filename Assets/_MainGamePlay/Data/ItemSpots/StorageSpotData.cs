using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocationProvider, IReservationProvider, IItemSpotInBuilding
{
    public override string ToString() => $"Storage {InstanceId}: {ItemContainer} {Reservation}";
    public int IndexInStoragePile;

    public BuildingData Building { get; set; }
    public LocationComponent Location { get; set; } = new();
    public ReservationComponent Reservation { get; set; } = new();
    public ItemContainerComponent ItemContainer { get; set; } = new();

    public StorageSpotData(StoragePileData pile, int pileIndex)
    {
        IndexInStoragePile = pileIndex;
        Building = pile.Building;
    }

    internal void OnBuildingDestroyed()
    {
        if (!ItemContainer.IsEmpty)
            Building.Town.AddItemToGround(ItemContainer.ClearItem(), Location);
    }
}