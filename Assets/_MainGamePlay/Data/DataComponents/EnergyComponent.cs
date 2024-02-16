using System;
using UnityEngine;

public interface IEnergyProvider
{
    EnergyComponent Energy { get; }
}
public delegate void OnEnergyRefilledEvent();

[Serializable]
public class EnergyComponent : BaseData
{
    public override string ToString() => $"Energy: {EnergyLevel}";

    // 0-1
    public float EnergyLevel;
    public float EnergyRestoredPerSecondWhenSleeping;

    public bool IsResting;
    [NonSerialized] public OnEnergyRefilledEvent OnEnergyRefilled;

    public void Update()
    {
        if (IsResting)
        {
            EnergyLevel = Mathf.Min(1, EnergyLevel + EnergyRestoredPerSecondWhenSleeping * GameTime.deltaTime);
            if (EnergyLevel == 1)
                OnEnergyRefilled?.Invoke();
        }
    }
}