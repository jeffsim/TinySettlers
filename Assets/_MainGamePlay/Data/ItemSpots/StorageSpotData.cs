using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocationProvider, IReservationProvider, IItemSpotInBuilding
{
    public override string ToString() => Location + " " + ItemContainer;

    [SerializeField] public BuildingData Building { get; set; }
    public int IndexInStoragePile;

    [SerializeField] public LocationComponent Location { get; set; }
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    [SerializeField] public ItemContainerComponent ItemContainer { get; set; } = new();

    public bool HasItem => ItemContainer.HasItem;
    public bool IsEmptyAndAvailable => ItemContainer.IsEmpty && !Reservation.IsReserved;

    public StorageSpotData(StoragePileData pile, int pileIndex)
    {
        IndexInStoragePile = pileIndex;
        Building = pile.Building;
        Location = new LocationComponent(pile.Location, 0, 0);
    }

    internal void OnBuildingDestroyed()
    {
        if (!ItemContainer.IsEmpty)
        {
            var item = ItemContainer.ClearItem();
            Building.Town.AddItemToGround(item, Location);
        }
    }
}