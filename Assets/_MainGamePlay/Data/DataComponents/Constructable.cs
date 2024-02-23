using System;
using UnityEngine;

public interface IConstructable
{
    Constructable Constructable { get; }
}

public enum ConstructionState { NotConstructed, UnderConstruction, FullyConstructed }

[Serializable]
public class Constructable : BaseData
{
    public override string ToString() => $"Constructable: {PercentConstructed}";

    public ConstructionState ConstructionState;
    public float PercentConstructed;
    public bool IsConstructed => ConstructionState == ConstructionState.FullyConstructed;

    public float PercentConstructedPerSecond = .1f;

    [NonSerialized] public Action OnConstructionStarted;
    [NonSerialized] public Action OnConstructionCompleted;

    [SerializeField] IConstructable Owner;

    public Constructable(ConstructableDefn defn, IConstructable owner)
    {
        Owner = owner;
    }

    internal void StartConstruction()
    {
        PercentConstructed = 0;
        ConstructionState = ConstructionState.UnderConstruction;
    }

    public void Update()
    {
        if (ConstructionState == ConstructionState.UnderConstruction)
        {
            PercentConstructed = Mathf.Min(1, PercentConstructed + PercentConstructedPerSecond * GameTime.deltaTime);
            if (PercentConstructed == 1)
            {
                ConstructionState = ConstructionState.FullyConstructed;
                OnConstructionCompleted?.Invoke();
            }
        }
    }
}