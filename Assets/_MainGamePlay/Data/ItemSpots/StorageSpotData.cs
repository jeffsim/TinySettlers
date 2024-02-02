using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData
{
    public BuildingData Building;
    [SerializeField] StoragePileData Pile;
    public int IndexInStoragePile;

    public LocationComponent Location;
    public ReservationComponent Reservation = new();
    public ItemContainerComponent ItemContainer = new();

    public virtual void UpdateWorldLoc() => Location.WorldLoc = Pile.Location.WorldLoc + Location.LocalLoc;
    public bool HasItem => ItemContainer.HasItem;
    public bool IsEmptyAndAvailable => ItemContainer.IsEmpty && !Reservation.IsReserved;

    public StorageSpotData(StoragePileData pile, Vector2 localLoc, int pileIndex)
    {
        Pile = pile;
        IndexInStoragePile = pileIndex;
        Building = pile.Building;
        Location = new LocationComponent(pile.Location, localLoc.x, localLoc.y);
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