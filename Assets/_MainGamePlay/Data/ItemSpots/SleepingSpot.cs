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

    public SleepingSpotData(BuildingData building, int index)
    {
        Building = building;
        Debug.Assert(building.Defn.SleepingSpots.Count > index, "building " + building.DefnId + " missing SleepingSpotData " + index);
    }

    public void Update()
    {
        // If a worker is sleeping here then reduce increase their energy gradually until full
    }
}