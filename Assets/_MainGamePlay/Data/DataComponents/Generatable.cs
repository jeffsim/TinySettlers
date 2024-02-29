using System;
using UnityEngine;

public interface IGeneratable
{
    Generatable Generatable { get; }
    public void OnGenerated();
}

[Serializable]
public class Generatable : BaseData
{
    public override string ToString() => $"Generatable: {PercentGenerated}";

    public float PercentGenerated;

    [NonSerialized] public Action OnGenerated;
    [NonSerialized] public Action OnPercentChanged;

    [SerializeField] IGeneratable Owner;
    [SerializeField] float SecondsToGenerate;
    public bool IsEnabled;

    public Generatable(GeneratableDefn defn, IGeneratable owner)
    {
        IsEnabled = defn.CanGenerate;
        if (!IsEnabled) return;
        Owner = owner;
        PercentGenerated = 0;
        SecondsToGenerate = defn.BaseSecondsToGenerate;
    }

    public void Update()
    {
        if (!IsEnabled) return;
        var oldPercent = PercentGenerated;
        PercentGenerated = Mathf.Min(1, PercentGenerated + GameTime.deltaTime / SecondsToGenerate);
        if (oldPercent != PercentGenerated)
            OnPercentChanged?.Invoke();
        if (PercentGenerated == 1)
        {
            OnGenerated?.Invoke();
            Owner.OnGenerated();

            PercentGenerated = 0;
        }
    }
}