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
    public GoodType GoodType = GoodType.explicitGood;
    public List<ResourceNeededForCraftingOrConstruction> ResourcesNeededForCrafting;
    public ItemClass ItemClass;
}
