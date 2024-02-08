using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldMapScene : SceneMgr
{
    public WorldMapTown WorldMapTownPrefab;
    public GameObject TownsFolder;

    public override void OnEnable()
    {
        base.OnEnable();
        if (gameDataMgr.GameData == null) // failed to load; already handled base base class
            return;

        updateCityStates();
    }

    private void updateCityStates()
    {
        TownsFolder.RemoveAllChildren();
        foreach (var world_town in gameDataMgr.GameData.World.World_Towns)
        {
            if (world_town.State == TownState.Undiscovered)
                continue;
            var townGO = Instantiate(WorldMapTownPrefab);
            townGO.Initialize(world_town, this, gameDataMgr.GameData.CurrentTown);
            townGO.transform.SetParent(TownsFolder.transform, false);
        }
    }

    public void OnMainClicked()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void OnUnlockAllClicked()
    {
        // Set all towns state to available
        foreach (var town in gameDataMgr.GameData.World.World_Towns)
            town.State = TownState.Available;
        gameDataMgr.SaveProfile();
        SceneManager.LoadScene("WorldMapScene", LoadSceneMode.Single); // reload
    }

    public void TownClicked(World_TownData town)
    {
        if (town.State == TownState.Available)
        {
            // Town is being entered for the first time; initialize it
            var townDefn = GameDefns.Instance.TownDefns[town.TownDefnId];
            var townData = new TownData(townDefn);
            townData.InitializeOnFirstEnter();
            gameDataMgr.SetCurrentTown(townData);
            town.State = TownState.InProgress; // will be saved below
        }

        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }
}