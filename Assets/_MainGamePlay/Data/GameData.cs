using System;

[Serializable]
public class GameData
{
    /// The Name with which this GameData is saved
    public string ProfileName;

    public WorldData World;

    public TownData CurrentTown;

    // Enables unique generation of ids that span saves
    public UniqueIdGenerator UniqueIdGenerator;

    public void InitializeNew()
    {
        UniqueIdGenerator = new UniqueIdGenerator();
        UniqueIdGenerator.Instance = UniqueIdGenerator;

        var worldDefn = GameDefns.Instance.WorldDefns["mainWorld"];
        World = new WorldData(worldDefn);
    }

    public void OnLoaded()
    {
        UniqueIdGenerator.Instance = UniqueIdGenerator;
        GameTime.UnPause();
    }

    internal void CurrentTownWonLost(bool won)
    {
        World.TownWonLost(CurrentTown, won);
        CurrentTown = null;
    }
}