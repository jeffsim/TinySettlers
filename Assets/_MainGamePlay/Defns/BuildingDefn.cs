using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum BuildingClass
{
    Unset, Camp, Other
}

[CreateAssetMenu(fileName = "BuildingDefn")]
public class BuildingDefn : BaseDefn
{
    public Color BuildingColor;
    public string FriendlyName;

    public BuildingClass BuildingClass = BuildingClass.Other;

    public Color AssignedWorkerColor;
    public string AssignedWorkerFriendlyName;

    // used in the editor
    public Color EditorColor;

    public bool PlayerCanMove = true;
    public bool PlayerCanDestroy = true;

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
    public List<Vector3> StorageAreaLocations;
    [ShowIf("CanStoreItems")]
    public int NumStorageAreas => StorageAreaLocations.Count;
    [ShowIf("CanStoreItems")]
    public int StorageAreaWidthAndHeight = 3;

    public bool WorkersCanFerryItems;

    public bool CanGatherResources;
    [ShowIf("CanGatherResources")]
    public List<ItemDefn> GatherableResources;

    public bool ResourcesCanBeGatheredFromHere;
    [ShowIf("ResourcesCanBeGatheredFromHere")]
    public List<ItemDefn> ResourcesThatCanBeGatheredFromHere;

    [ShowIf("ResourcesCanBeGatheredFromHere")]
    public List<Vector3> GatheringSpots;
}
