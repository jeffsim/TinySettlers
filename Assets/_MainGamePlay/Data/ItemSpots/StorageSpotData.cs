using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocation, IReservable, IItemSpotInBuilding
{
    public override string ToString() => $"Storage {InstanceId}: {ItemContainer} {Reservable}";
    public int IndexInStoragePile;

    [SerializeField] public BuildingData Building { get; set; }
    [SerializeField] public Location Location { get; set; } = new();
    [SerializeField] public Reservable Reservable { get; set; }
    [SerializeField] public SingleContainable ItemContainer { get; set; } = new();

    public StorageSpotData(StoragePileData pile, int pileIndex)
    {
        IndexInStoragePile = pileIndex;
        Building = pile.Building;
        Reservable = new(this);
    }

    internal void OnBuildingDestroyed()
    {
        if (!ItemContainer.IsEmpty)
            Building.Town.AddItemToGround(ItemContainer.ClearItem(), Location);
    }
}