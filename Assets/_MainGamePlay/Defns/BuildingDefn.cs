using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum BuildingClass
{
    Unset, Camp, Other
}

[Serializable]
public class StorageAreaDefn
{
    public Vector3 Location;
    public Vector2Int StorageAreaSize = new(3, 3);

    // A storageArea is displayed as an NxM grid; before each cell was a single storagespot.  However, I want to support storing multiple items
    // in each 'cell' e.g. a 3x3x3 pile of woodplanks.  Rather than dealing with multiple reserverations per StorageSpot, at storagearea
    // creation time, I'll create a [3x3x3 grid] of storage spot in each cell, and the View can render them accordingly.
    public Vector3Int StoragePileSize = new(3, 3, 3);
}

[CreateAssetMenu(fileName = "BuildingDefn")]
public class BuildingDefn : BaseDefn
{
    public Color BuildingColor;
    public Color BuildingBottomColor;
    public string FriendlyName;
    public bool IsTestBuilding = false;
    public BuildingClass BuildingClass = BuildingClass.Other;

    public Color AssignedWorkerColor;
    public string AssignedWorkerFriendlyName;

    public bool HasWorkers = true;
    [ShowIf("HasWorkers")]
    public int MaxWorkers = 4;
    
    // e.g. Camp and House: building it grants additional max workers to the Town
    public int MaxTownWorkersIncreasedWhenBuilt = 0;

    // used in the editor
    public Color EditorColor;

    public bool PlayerCanMove = true;
    public bool PlayerCanDestroy = true;
    public bool PlayerCanPause = true;

    public bool CanBeConstructed;
    [ShowIf("CanBeConstructed")]
    public List<ResourceNeededForCraftingOrConstruction> ResourcesNeededForConstruction;
    [ShowIf("CanBeConstructed")]
    public int NumConstructorSpots;

    public bool CanCraft;
    [ShowIf("CanCraft")]
    public List<ItemDefn> CraftableItems; // IMPORTANT: Crafted items in a building shouldn't have those items be used in other crafted items in the building.  messes up storage AI
    [ShowIf("CanCraft")]
    public List<Vector3> CraftingSpots;

    public bool CanStoreItems;
    [ShowIf("CanStoreItems")]
    public bool IsPrimaryStorage; // If true, then don't move items out of this storage unless critical.  applies to storage, camp.
    [ShowIf("CanStoreItems")]
    public List<StorageAreaDefn> StorageAreas = new();

    public bool WorkersCanFerryItems;

    public bool CanGatherResources;
    [ShowIf("CanGatherResources")]
    public List<ItemDefn> GatherableResources;

    public bool ResourcesCanBeGatheredFromHere;
    [ShowIf("ResourcesCanBeGatheredFromHere")]
    public List<ItemDefn> ResourcesThatCanBeGatheredFromHere;

    [ShowIf("ResourcesCanBeGatheredFromHere")]
    public List<Vector3> GatheringSpots;

    public bool CanSellGoods;
    [ShowIf("CanSellGoods")]
    public List<ItemDefn> GoodsThatCanBeSold;
}
