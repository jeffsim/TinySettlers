using System;
using UnityEngine;

[Serializable]
public class StorageSpotData : BaseData, ILocation, IReservable, IContainerInBuilding
{
    public override string ToString() => $"Storage {InstanceId}: {Container} {Reservable}";
    public int IndexInStoragePile;

    [SerializeField] public BuildingData Building { get; set; }
    [SerializeField] public Location Location { get; set; } = new();
    [SerializeField] public Reservable Reservable { get; set; }
    [SerializeField] public Container Container { get; set; } = new();

    public StorageSpotData(StoragePileData pile, int pileIndex)
    {
        IndexInStoragePile = pileIndex;
        Building = pile.Building;
        Reservable = new(this);
    }

    internal void OnBuildingDestroyed()
    {
        if (!Container.IsEmpty)
            Building.Town.AddItemToGround(Container.ClearItems(), Location);
    }
}