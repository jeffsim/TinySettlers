using UnityEngine;

public class SceneMgr : MonoBehaviour
{
    protected GameDataMgr gameDataMgr;

    protected GameDefns gameDefns;

    // If false, then the current scene can exist without a profile loaded.  e.g.; main and settings do not 
    // require a profile, while in-game scenes do.
    public virtual bool RequiresProfile => true;

    public virtual void OnEnable()
    {
        GameTime.IsTest = false;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        gameDataMgr = FindFirstObjectByType<GameDataMgr>(FindObjectsInactive.Exclude);
        gameDefns = FindFirstObjectByType<GameDefns>(FindObjectsInactive.Exclude);

        Debug.Assert(gameDataMgr != null, "Failed to find GameDataMgr");
        Debug.Assert(gameDefns != null, "Failed to find GameDefns");
    }

    public void SetGameSpeed (float speed)
    {
        GameTime.timeScale = speed;
    }
} 