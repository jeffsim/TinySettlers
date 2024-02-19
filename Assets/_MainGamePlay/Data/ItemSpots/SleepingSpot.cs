using System;
using UnityEngine;

[Serializable]
public class SleepingSpotData : BaseData, ILocationProvider, IReservationProvider
{
    public override string ToString() => $"Sleeping {InstanceId}: {Reservation}";
    public int IndexInStoragePile;

    [SerializeField] public BuildingData Building { get; set; }
    [SerializeField] public LocationComponent Location { get; set; } = new();
    [SerializeField] public ReservationComponent Reservation { get; set; } = new();

    public Vector3 LocOffset;

    public SleepingSpotData(BuildingData building, int index)
    {
        Building = building;
        Debug.Assert(building.Defn.SleepingSpots.Count > index, "building " + building.DefnId + " missing SleepingSpotData " + index);
        var loc = building.Defn.SleepingSpots[index];
        LocOffset = new(loc.x, Settings.ItemSpotsY, loc.y);
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