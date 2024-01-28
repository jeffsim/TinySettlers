using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

// UGH - this exists so that TownDefnEditor can work
[Serializable]
public class GameDefnsMgr
{
    [SerializeReference] public Dictionary<string, BuildingDefn> BuildingDefns = new Dictionary<string, BuildingDefn>();
    [SerializeReference] public Dictionary<string, ItemDefn> ItemDefns = new Dictionary<string, ItemDefn>();
    [SerializeReference] public Dictionary<string, TileDefn> TileDefns = new Dictionary<string, TileDefn>();
    [SerializeReference] public Dictionary<string, TownDefn> TownDefns = new Dictionary<string, TownDefn>();
    [SerializeReference] public Dictionary<string, WorkerDefn> WorkerDefns = new Dictionary<string, WorkerDefn>();
    [SerializeReference] public Dictionary<string, WorldDefn> WorldDefns = new Dictionary<string, WorldDefn>();
    [SerializeReference] public Dictionary<string, GameSettingsDefn> GameSettingsDefns = new();

    public void RefreshDefns()
    {
        // Find all Defn objects and add them.
        // TODO (PERF, LATER): This makes my development life easier, but when I get closer to prod, these should be stored 
        // persistently in the GameDefns prefab so that I don't have to do this on every scene load
        loadDefns("Buildings", BuildingDefns);
        loadDefns("Tiles", TileDefns);
        loadDefns("Items", ItemDefns);
        loadDefns("Towns", TownDefns);
        loadDefns("Workers", WorkerDefns);
        loadDefns("Worlds", WorldDefns);
        loadDefns("GameSettings", GameSettingsDefns);

        if (GameTime.IsTest)
        {
            loadDefns("Towns/Test", TownDefns);
            loadDefns("Buildings/Test", BuildingDefns);
        }
    }

    private void loadDefns<T>(string folderName, Dictionary<string, T> defnDict) where T : BaseDefn
    {
        defnDict.Clear();
        var defns = Resources.LoadAll<T>("Defns/" + folderName);
        foreach (var defn in defns)
            defnDict[defn.Id] = defn as T;
    }
}

public class GameDefns : SerializedMonoBehaviour
{
    public static GameDefns Instance;

    private GameDefnsMgr GameDefnsMgr;
    public Dictionary<string, BuildingDefn> BuildingDefns => GameDefnsMgr.BuildingDefns;
    public Dictionary<string, ItemDefn> ItemDefns => GameDefnsMgr.ItemDefns;
    public Dictionary<string, TileDefn> TileDefns => GameDefnsMgr.TileDefns;
    public Dictionary<string, TownDefn> TownDefns => GameDefnsMgr.TownDefns;
    public Dictionary<string, WorkerDefn> WorkerDefns => GameDefnsMgr.WorkerDefns;
    public Dictionary<string, WorldDefn> WorldDefns => GameDefnsMgr.WorldDefns;
    public Dictionary<string, GameSettingsDefn> GameSettingsDefns => GameDefnsMgr.GameSettingsDefns;

    void Awake()
    {
        if (!GameTime.IsTest)
        {
            GameDefns[] objs = FindObjectsByType<GameDefns>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (objs.Length > 1)
                Destroy(gameObject);
            DontDestroyOnLoad(gameObject);
        }
        Instance = this;
        GameDefnsMgr = new GameDefnsMgr();
        GameDefnsMgr.RefreshDefns();
    }

    void OnEnable()
    {
        Instance = this;
        GameDefnsMgr.RefreshDefns();
    }

    public void Test_ForceAwake()
    {
        if (Instance == null || GameDefnsMgr == null)
            Awake();
        else
            GameDefnsMgr.RefreshDefns();
    }
}
