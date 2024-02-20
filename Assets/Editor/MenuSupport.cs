
using UnityEditor;
using UnityEngine;

public class MenuSupport
{
    [MenuItem("TinySettlers/Delete All Profiles")]
    public static void DeleteCurrentProfile()
    {
        var gameDataMgr = Object.FindFirstObjectByType<GameDataMgr>(FindObjectsInactive.Exclude);
        if (gameDataMgr == null)
        {
            Debug.Log("No GameDataMgr found");
            return;
        }
        gameDataMgr.DeleteAllProfiles();
        Debug.Log("All Profiles deleted");
    }

    [MenuItem("TinySettlers/Open Profiles folder")]
    public static void OpenProfilesFolder()
    {
        var gameDataMgr = Object.FindFirstObjectByType<GameDataMgr>(FindObjectsInactive.Exclude);
        if (gameDataMgr == null)
        {
            Debug.Log("No GameDataMgr found");
            return;
        }
        EditorUtility.RevealInFinder(gameDataMgr.ProfilesFolderName);
    }

    [MenuItem("TinySettlers/Show Game Settings %g")]
    public static void ShowGameSettings()
    {
        var path = "Assets/Resources/Defns/GameSettings/default.asset";
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameSettingsDefn>(path);
    }

}
