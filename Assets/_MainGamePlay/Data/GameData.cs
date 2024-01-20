using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    /// The Name with which this GameData is saved
    public string ProfileName;

    public TownData CurrentTown;

    public List<TownData> Towns = new List<TownData>();

    public float lastGameTime;

    // Enables unique generation of ids that span saves
    public UniqueIdGenerator UniqueIdGenerator;

    public void InitializeNew()
    {
        UniqueIdGenerator = new UniqueIdGenerator();
        UniqueIdGenerator.Instance = UniqueIdGenerator;

        var world = GameDefns.Instance.WorldDefns["mainWorld"];
        foreach (var worldTownDefn in world.Towns)
            Towns.Add(new TownData(worldTownDefn.Town, worldTownDefn.StartingState));

        CurrentTown = null;
    }

    public void OnLoaded()
    {
        UniqueIdGenerator.Instance = UniqueIdGenerator;
        GameTime.time = lastGameTime;
    }
}