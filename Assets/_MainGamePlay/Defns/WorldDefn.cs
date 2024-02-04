using System;
using System.Collections.Generic;
using UnityEngine;

public enum TownState { Undiscovered, Locked, Available, InProgress, Completed };

[Serializable]
public class World_TownDefn
{
    public TownDefn Town;
    public TownState StartingState;
    public List<TownDefn> WinningUnlocks = new();
}

[CreateAssetMenu(fileName = "WorldDefn")]
public class WorldDefn : BaseDefn
{
    public string FriendlyName;

    public List<World_TownDefn> Towns = new();
}
