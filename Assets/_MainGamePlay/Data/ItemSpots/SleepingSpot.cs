using System;
using UnityEngine;

[Serializable]
public class SleepingSpotData : BaseData, ILocation, IReservable
{
    public override string ToString() => $"Sleeping {InstanceId}: {Reservable}";
    public int IndexInStoragePile;

    [SerializeField] public BuildingData Building { get; set; }
    [SerializeField] public Location Location { get; set; } = new();
    [SerializeField] public Reservable Reservable { get; set; }

    public Vector3 LocOffset;

    public SleepingSpotData(BuildingData building, int index)
    {
        Building = building;
        Debug.Assert(building.Defn.SleepingSpots.Count > index, "building " + building.DefnId + " missing SleepingSpotData " + index);
        var loc = building.Defn.SleepingSpots[index];
        LocOffset = new(loc.x, Settings.Current.ItemSpotsY, loc.y);
        Reservable = new(this);
    }

    public void UpdateWorldLoc()
    {
        Location.SetWorldLoc(Building.Location.WorldLoc + LocOffset);
    }

    public void Update()
    {
        // If a worker is sleeping here then reduce increase their energy gradually until full
    }
}