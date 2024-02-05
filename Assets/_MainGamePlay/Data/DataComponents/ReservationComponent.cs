using System;
using UnityEngine;

public interface IReservationProvider
{
    ReservationComponent Reservation { get; }
}

[Serializable]
public class ReservationComponent : BaseData
{
    public override string ToString() => IsReserved ? "Reserved by " + ReservedBy : "Not reserved";

    public WorkerData ReservedBy;
    public bool IsReserved => ReservedBy != null;

    public void Unreserve()
    {
        Debug.Assert(IsReserved, "Unreserving already unreserved spot (" + InstanceId + ")");
        ReservedBy = null;
    }

    public void ReserveBy(WorkerData worker)
    {
        Debug.Assert(!IsReserved, "Reserving already reserved storage spot (" + InstanceId + ")");
        Debug.Assert(worker != null, "Null reserver (" + InstanceId + ")");

        ReservedBy = worker;
    }
}