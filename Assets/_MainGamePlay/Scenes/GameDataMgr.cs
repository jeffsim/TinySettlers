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

    void Awake()
    {
        GameDataMgr[] objs = FindObjectsByType<GameDataMgr>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (objs.Length > 1)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    internal void OnEnable()
    {
        if (GameData == null)
        {
            var lastLoadedProfile = PlayerPrefs.GetString("LoadLoadedProfile", "");
            if (lastLoadedProfile != "")
            {
                // load profile
                try
                {
                    LoadProfile(lastLoadedProfile);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error loading profile: " + e.Message);
                    PlayerPrefs.SetString("LoadLoadedProfile", "");
                }
            }
            if (GameData == null)
            {
                // no profile exists. or failed to load it.  Allowed only on MainScene; if on any other scene, revert to main
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

        if (GameData.CurrentTown != null)
            GameData.CurrentTown.lastGameTime = GameTime.time;

        var fileName = GetProfileFileName(GameData.ProfileName);
        byte[] bytes = SerializationUtility.SerializeValue(GameData, Settings.Current.UseBinarySaveFiles ? DataFormat.Binary : DataFormat.JSON);
        File.WriteAllBytes(fileName, bytes);
    }

    internal void ReloadProfile()
    {
        LoadProfile(GameData.ProfileName);
    }

    internal void LoadProfile(string profileName)
    {
        byte[] bytes = File.ReadAllBytes(GetProfileFileName(profileName));
        GameData = SerializationUtility.DeserializeValue<GameData>(bytes, Settings.Current.UseBinarySaveFiles ? DataFormat.Binary : DataFormat.JSON);

        GameData.OnLoaded();
        PlayerPrefs.SetString("LoadLoadedProfile", profileName);
    }

    private string GetProfileFileName(string fileName)
    {
        return Path.Combine(ProfilesFolderName, fileName);
    }
}
