using System;
using UnityEngine;

[Serializable]
public class BaseDefn : ScriptableObject
{
    public override string ToString() => Id;

    public string Id;
}
