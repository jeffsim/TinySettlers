using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Town_BuildingStartingItemDefn
{
    public ItemDefn Item;
    public int Count;
}

[Serializable]
public class Town_BuildingDefn
{
    public bool IsEnabled = true; // used for debugging town w/o deleting buildings
    public BuildingDefn Building;
    public string TestId;
    public int TileX;
    public int TileY;

    public List<Town_BuildingStartingItemDefn> StartingItemsInBuilding;
    public int NumWorkersStartAtBuilding;
}

[CreateAssetMenu(fileName = "TownDefn")]
public class TownDefn : BaseDefn
{
    public string FriendlyName;

    public int Width;
    public int Height;

    public string Tiles;

    // Position in the WorldMap
    public int WorldX;
    public int WorldY;

    public List<Town_BuildingDefn> Buildings = new List<Town_BuildingDefn>();
}
