using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceNeededForCraftingOrConstruction
{
    public ItemDefn Item;
    public int Count;
}

public enum GoodType { implicitGood = 1, explicitGood = 2 };

[CreateAssetMenu(fileName = "ItemDefn")]
public class ItemDefn : BaseDefn
{
    public string FriendlyName;
    public Color Color;
    public Item VisualPrefab;

    public GoodType GoodType = GoodType.explicitGood;
    public List<ResourceNeededForCraftingOrConstruction> ResourcesNeededForCrafting;
    public ItemClass ItemClass;

    // sold for this many gold.  can be increased via research, etc
    public int BaseSellPrice = 1;

    // Heavy objects slow down the worker carrying them
    public float CarryingSpeedModifier = 1.0f;

    // Only applicable to e.g. potatoes, trees
    public float SecondsToGrow = 1;
}
