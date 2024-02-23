using System;
using UnityEngine;

public interface IPausable
{
    PausableComponent Pausable { get; }
}

[Serializable]
public class PausableComponent : BaseData
{
    public override string ToString() => $"Pausable {IsPaused}";

    public bool CanBePaused;

    public bool IsPaused;

    // These CANNOT be subscribed to by other Data classes as they are not serialized. They can
    // only be subscribed to by the View classes which re-establish the subscriptions every time
    // (These aren't serialized as 'Action' is a unity concept that isn't serialized outside of
    // scriptableobjects/monobehaviours)
    [NonSerialized] public Action OnPauseToggled;

    [SerializeField] BuildingData damnit;

    public PausableComponent(PausableDefn defn, BuildingData fuckinHACK)
    {
        CanBePaused = defn.CanBePaused;
        
        damnit = fuckinHACK;
    }

    public void TogglePaused()
    {
        IsPaused = !IsPaused;
        OnPauseToggled?.Invoke();

        damnit.OnPauseToggled();
    }

    public void Pause() { if (!IsPaused) TogglePaused(); }
    public void Unpause() { if (IsPaused) TogglePaused(); }
}