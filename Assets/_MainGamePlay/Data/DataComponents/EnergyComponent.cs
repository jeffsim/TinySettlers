using System;
using UnityEngine;

public interface IEnergyProvider
{
    EnergyComponent Energy { get; }
}

[Serializable]
public class EnergyComponent : BaseData
{
    public override string ToString() => $"Energy: {EnergyLevel}";

    // 0-1
    public float EnergyLevel;

    public float EnergyRestoredPerSecondWhenResting = 0.05f;
    public float EnergyDepletedPerSecondWhenWorking = 0.01f;

    public bool IsResting;

    [NonSerialized] public Action OnEnergyFullyRestored;
    [NonSerialized] public Action OnEnergyExhausted;

    public void Update()
    {
        if (IsResting && EnergyLevel < 1)
        {
            // Resting; restore energy
            EnergyLevel = Mathf.Min(1, EnergyLevel + EnergyRestoredPerSecondWhenResting * GameTime.deltaTime);
            if (EnergyLevel == 1)
                OnEnergyFullyRestored?.Invoke();
        }
        else if (!IsResting && EnergyLevel > 0)
        {
            // Working; deplete energy
            EnergyLevel = Mathf.Max(0, EnergyLevel - EnergyDepletedPerSecondWhenWorking * GameTime.deltaTime);
            if (EnergyLevel == 0)
                OnEnergyExhausted?.Invoke();
        }
    }

    internal void FillUp()
    {
        EnergyLevel = 1;
        OnEnergyFullyRestored?.Invoke();
    }
}