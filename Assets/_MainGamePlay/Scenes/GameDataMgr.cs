using System;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameDataMgr : MonoBehaviour
{
    [ShowInInspector][NonSerialized] public GameData GameData;

    public string ProfilesFolderName => Path.Combine(Application.persistentDataPath, "Profiles");
    bool UseBinarySaveFiles = false;

    void Awake()
    {
        GameDataMgr[] objs = GameObject.FindObjectsByType<GameDataMgr>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (objs.Length > 1)
            Destroy(this.gameObject);
        DontDestroyOnLoad(this.gameObject);
    }

    internal void OnEnable()
    {
        if (GameData == null)
        {
            var lastLoadedProfile = PlayerPrefs.GetString("LoadLoadedProfile", "");
            if (lastLoadedProfile != "")
            {
                // load profile
                LoadProfile(lastLoadedProfile);
            }
            else
            {
                // no profile exists.  Allowed only on MainScene; if on any other scene, revert to main
                var curScene = FindFirstObjectByType<SceneMgr>();
                if (curScene == null || curScene.RequiresProfile)
                {
                    Debug.Log("in scene '" + curScene.name + "' without current profile. Returning to mainscene");
                    SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
                }
            }
        }
    }

    internal void SetCurrentTown(TownData town)
    {
        GameData.CurrentTown = town;
        SaveProfile();
    }

    private void EnsureProfilesFolderExists()
    {
        if (!Directory.Exists(ProfilesFolderName))
            Directory.CreateDirectory(ProfilesFolderName);
    }

    public bool ProfileExists(string profileName)
    {
        var fileName = GetProfileFileName(profileName);
        return File.Exists(fileName);
    }

    public void DeleteAllProfiles()
    {
        File.Delete(GetProfileFileName("Jeff"));
        File.Delete(GetProfileFileName("Jenn"));
        File.Delete(GetProfileFileName("Luke"));

        GameData = null;
        PlayerPrefs.SetString("LoadLoadedProfile", "");
    }

    internal void CreateProfile(string profileName)
    {
        GameData = new GameData() { ProfileName = profileName };
        GameData.InitializeNew();
        SaveProfile();

        PlayerPrefs.SetString("LoadLoadedProfile", profileName);
    }

    public void SaveProfile()
    {
        EnsureProfilesFolderExists();

        var fileName = GetProfileFileName(GameData.ProfileName);
        byte[] bytes = SerializationUtility.SerializeValue(GameData, UseBinarySaveFiles ? DataFormat.Binary : DataFormat.JSON);
        File.WriteAllBytes(fileName, bytes);
    }

    internal void ReloadProfile()
    {
        LoadProfile(GameData.ProfileName);
    }

    internal void LoadProfile(string profileName)
    {
        byte[] bytes = File.ReadAllBytes(GetProfileFileName(profileName));
        GameData = SerializationUtility.DeserializeValue<GameData>(bytes, UseBinarySaveFiles ? DataFormat.Binary : DataFormat.JSON);

        GameData.OnLoaded();
        PlayerPrefs.SetString("LoadLoadedProfile", profileName);
    }

    private string GetProfileFileName(string fileName)
    {
        return Path.Combine(ProfilesFolderName, fileName);
    }
}
