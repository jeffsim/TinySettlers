using System;
using UnityEngine;

public interface IReservable
{
    Reservable Reservable { get; }
}

[Serializable]
public class Reservable : BaseData
{
    public override string ToString() => $"Reservable: {(IsReserved ? "Reserved by " + ReservedBy : "Not reserved")}";

    public WorkerData ReservedBy;
    public bool IsReserved => ReservedBy != null;
    [SerializeField] IReservable Owner;

    public Reservable(IReservable owner)
    {
        Owner = owner;
    }

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