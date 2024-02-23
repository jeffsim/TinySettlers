using System;

public enum ConstructionState { NotStarted, UnderConstruction, FullyConstructed };

[Serializable]
public class BuildingConstructionMgr
{
    // public ConstructionState ConstructionState;
    // public float PercentBuilt; 
    // public bool IsConstructed => !(Defn.CanBeConstructed) || (ConstructionState == ConstructionState.FullyConstructed);

    // public List<NeedData> ConstructionNeeds = new();

    public BuildingData Building;

    public BuildingConstructionMgr(BuildingData building)
    {
        Building = building;

        // Add need for construction and materials if the building needs to be constructed
        // if (!IsConstructed)
        // {
        //     ConstructionNeeds.Add(new NeedData(this, NeedType.ConstructionWorker, null, Defn.NumConstructorSpots));
        //     foreach (var resource in Defn.ResourcesNeededForConstruction)
        //         ConstructionNeeds.Add(new NeedData(this, NeedType.CraftingOrConstructionMaterial, resource));
        //     Needs.AddRange(ConstructionNeeds);
        // }
    }
}