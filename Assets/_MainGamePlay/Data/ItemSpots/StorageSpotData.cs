using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocationProvider, IReservationProvider, IItemSpotInBuilding
{
    public override string ToString() => Location + " " + ItemContainer;
    public int IndexInStoragePile;

    [SerializeField] public BuildingData Building { get; set; }
    [SerializeField] public LocationComponent Location { get; set; } = new();
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    [SerializeField] public ItemContainerComponent ItemContainer { get; set; } = new();

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