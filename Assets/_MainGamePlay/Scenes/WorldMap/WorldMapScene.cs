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
        foreach (var town in gameDataMgr.GameData.Towns)
        {
            if (town.State == TownState.Undiscovered)
                continue;

            var townGO = WorldMapTown.Instantiate<WorldMapTown>(WorldMapTownPrefab);
            townGO.Initialize(town, this);
            townGO.transform.SetParent(TownsFolder.transform, false);
        }
    }

    public void OnMainClicked()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void TownClicked(TownData town)
    {
        if (town.State == TownState.Available)
        {
            // Town is being entered for the first time; initialize it
            town.InitializeOnFirstEnter();
            town.State = TownState.InProgress; // will be saved below
        }

        // Track which town the player last entered so that they can quickly return there on next launch
        gameDataMgr.SetCurrentTown(town);

        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }
}