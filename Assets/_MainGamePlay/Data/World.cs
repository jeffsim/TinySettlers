using System;
using System.Collections.Generic;
using System.Diagnostics;

[Serializable]
public class World_TownData
{
    public string TownDefnId;
    public TownState State;
    public bool CanEnter => State == TownState.Available || State == TownState.InProgress;
}

[Serializable]
public class WorldData : BaseData
{
    private WorldDefn _defn;
    public WorldDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.WorldDefns[DefnId];
    public string DefnId;
    public List<World_TownData> World_Towns = new();
    public World_TownData GetWorldTown(string defnId) => World_Towns.Find(t => t.TownDefnId == defnId);

    public WorldData(WorldDefn worldDefn)
    {
        DefnId = worldDefn.Id;

        foreach (var worldTownDefn in worldDefn.Towns)
            World_Towns.Add(new World_TownData { TownDefnId = worldTownDefn.Town.Id, State = worldTownDefn.StartingState });
    }

    public void TownWonLost(TownData town, bool won)
    {
        var worldTownDefn = Defn.Towns.Find(t => t.Town.Id == town.DefnId);

        var world_town = GetWorldTown(town.DefnId);
        world_town.State = won ? TownState.Completed : TownState.Available;
        if (won)
        {
            // Unlock previously locked towns that are unlocked when town is won
            foreach (var unlockDefn in worldTownDefn.WinningUnlocks)
            {
                var unlockTown = GetWorldTown(unlockDefn.Id);
                Debug.Assert(unlockTown != null);
                unlockTown.State = TownState.Available;
            }
        }
    }
}