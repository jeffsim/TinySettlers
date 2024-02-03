using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocationProvider, IReservationProvider
{
    public override string ToString() => Location + " " + ItemContainer;

    public BuildingData Building;
    [SerializeField] StoragePileData Pile;
    public int IndexInStoragePile;

    [SerializeField] public LocationComponent Location { get; set; }
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();
    public ItemContainerComponent ItemContainer = new();

    public bool HasItem => ItemContainer.HasItem;
    public bool IsEmptyAndAvailable => ItemContainer.IsEmpty && !Reservation.IsReserved;

    public StorageSpotData(StoragePileData pile, int pileIndex)
    {
        Pile = pile;
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