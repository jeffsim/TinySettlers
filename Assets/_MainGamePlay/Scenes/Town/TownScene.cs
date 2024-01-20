using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TownScene : SceneWithMap
{
    public TextMeshProUGUI TownName;
    public NeedsGraph NeedsGraph;

    public override void OnEnable()
    {
        base.OnEnable();
        if (gameDataMgr.GameData == null) // failed to load; already handled in base class
            return;

        if (gameDataMgr.GameData.CurrentTown == null)
        {
            Debug.Log("In TownScene but no active Town; likely debugging.  Returning to MainScene");
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
            return;
        }

        TownName.text = gameDataMgr.GameData.CurrentTown.Defn.FriendlyName;

        CreateMap(gameDataMgr.GameData.CurrentTown);

        NeedsGraph.ShowForScene(this);
    }

    public void Update()
    {
        if (gameDataMgr.GameData.CurrentTown == null) return;
        if (GameTime.timeScale == 0) return;
        gameDataMgr.GameData.CurrentTown.Update();
    }

    public void OnTownCompleted()
    {
        gameDataMgr.SetCurrentTown(null);

        // show dialog
    }

    public void OnMainClicked()
    {
        gameDataMgr.SetCurrentTown(null);
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void OnWorldMapClicked()
    {
        gameDataMgr.SetCurrentTown(null);
        SceneManager.LoadScene("WorldMapScene", LoadSceneMode.Single);
    }

    public void OnSaveClicked()
    {
        gameDataMgr.SaveProfile();
    }

    public void OnLoadClicked()
    {
        gameDataMgr.ReloadProfile();
        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }

    public void OnResetClicked()
    {
        gameDataMgr.GameData.CurrentTown = new TownData(gameDataMgr.GameData.CurrentTown.Defn, gameDataMgr.GameData.CurrentTown.State);
        gameDataMgr.GameData.CurrentTown.InitializeOnFirstEnter();

        gameDataMgr.SaveProfile();
        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }
}