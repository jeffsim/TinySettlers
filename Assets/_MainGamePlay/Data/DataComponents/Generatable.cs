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

    [SerializeField] IGeneratable Owner;
    [SerializeField] float SecondsToGenerate;

    public Generatable(GeneratableDefn defn, IGeneratable owner)
    {
        Owner = owner;
        PercentGenerated = 0;
        SecondsToGenerate = defn.BaseSecondsToGenerate;
    }

    public void Update()
    {
        PercentGenerated = Mathf.Min(1, PercentGenerated + GameTime.deltaTime / SecondsToGenerate);
        if (PercentGenerated == 1)
        {
            OnGenerated?.Invoke();
            Owner.OnGenerated();

            PercentGenerated = 0;
        }
    }
}