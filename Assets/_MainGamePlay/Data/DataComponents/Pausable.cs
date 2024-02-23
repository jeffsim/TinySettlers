using System;
using UnityEngine;

public interface IPausable
{
    Pausable Pausable { get; }
    public void OnPauseToggled();
}

[Serializable]
public class Pausable : BaseData
{
    public override string ToString() => $"Pausable: {IsPaused}";

    public bool CanBePaused;
    public bool IsPaused;

    // These CANNOT be subscribed to by other Data classes as they are not serialized. They can
    // only be subscribed to by the View classes which re-establish the subscriptions every time
    // (These aren't serialized as 'Action' is a unity concept that isn't serialized outside of
    // scriptableobjects/monobehaviours)
    [NonSerialized] public Action OnPauseToggled;

    [SerializeField] IPausable Owner;

    public Pausable(PausableDefn defn, IPausable owner)
    {
        CanBePaused = defn.CanBePaused;
        Owner = owner;
    }

    public void TogglePaused()
    {
        IsPaused = !IsPaused;
        OnPauseToggled?.Invoke();

        // ffs
        Owner.OnPauseToggled();
    }

    public void Pause() { if (!IsPaused) TogglePaused(); }
    public void Unpause() { if (IsPaused) TogglePaused(); }
}