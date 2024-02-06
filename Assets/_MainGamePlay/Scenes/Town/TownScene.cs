using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TownScene : SceneWithMap
{
    public TextMeshProUGUI TownName;
    public NeedsGraph NeedsGraph;

    // Sigh; without this, Load and Reset will call scene.Load and somehow and Update is called before Enable (!?)
    bool wthEnableRan;

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
        if (GameDefns.Instance.TownDefns == null || GameDefns.Instance.TownDefns == null)
        {
            Debug.Log("BAD STATE.  Not sure how.  Returning to MainScene");
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
            return;
        }
        gameDataMgr.GameData.CurrentTown.OnLoaded();
        TownName.text = gameDataMgr.GameData.CurrentTown.Defn.FriendlyName;

        CreateMap(gameDataMgr.GameData.CurrentTown);

        NeedsGraph.ShowForScene(this);
        wthEnableRan = false;
    }

    public void Update()
    {
        if (wthEnableRan) return;
        if (gameDataMgr.GameData.CurrentTown == null) return;
        if (GameTime.timeScale == 0) return;
        gameDataMgr.GameData.CurrentTown.Update();

        // right click = hide all dialogs
        if (Input.GetMouseButtonDown(1)) HideAllDialogs();

        if (Input.GetKeyDown(KeyCode.Alpha0)) SetGameSpeed(.25f);
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetGameSpeed(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetGameSpeed(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetGameSpeed(4);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetGameSpeed(8);
    }

    public void OnTownCompleted()
    {
        gameDataMgr.SetCurrentTown(null);

        // show dialog
    }

    public void OnMainClicked()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void OnFakeWinLoseClicked(bool won)
    {
        gameDataMgr.GameData.CurrentTownWonLost(won);

        SceneManager.LoadScene("WorldMapScene", LoadSceneMode.Single);
    }

    public void OnWorldMapClicked()
    {
        SceneManager.LoadScene("WorldMapScene", LoadSceneMode.Single);
    }

    public void OnSaveClicked()
    {
        gameDataMgr.SaveProfile();
    }

    public void OnLoadClicked()
    {
        wthEnableRan = true;
        gameDataMgr.ReloadProfile();
        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }

    public void OnResetClicked()
    {
        gameDataMgr.GameData.CurrentTown = new TownData(gameDataMgr.GameData.CurrentTown.Defn);
        gameDataMgr.GameData.CurrentTown.InitializeOnFirstEnter();

        gameDataMgr.SaveProfile();
        wthEnableRan = true;
        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }
}