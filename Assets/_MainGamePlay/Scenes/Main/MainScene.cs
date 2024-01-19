using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainScene : SceneMgr
{
    public TextMeshProUGUI ProfileName;

    public GameObject Load1Button;
    public GameObject Load2Button;
    public GameObject Load3Button;

    public GameObject Create1Button;
    public GameObject Create2Button;
    public GameObject Create3Button;

    public Button MapButton;
    public Button EnterLastTownButton;

    // Override this to allow this scene to work without a currently loaded profile
    public override bool RequiresProfile => false;

    public override void OnEnable()
    {
        base.OnEnable();
        updateProfileInfo();
    }

    private void updateProfileInfo()
    {
        var curProfile = gameDataMgr.GameData;
        if (curProfile != null)
            ProfileName.text = "Profile " + curProfile.ProfileName;
        else
            ProfileName.text = "No profile";

        Load1Button.SetActive(gameDataMgr.ProfileExists("Jeff"));
        Load2Button.SetActive(gameDataMgr.ProfileExists("Jenn"));
        Load3Button.SetActive(gameDataMgr.ProfileExists("Luke"));
        Create1Button.SetActive(!gameDataMgr.ProfileExists("Jeff"));
        Create2Button.SetActive(!gameDataMgr.ProfileExists("Jenn"));
        Create3Button.SetActive(!gameDataMgr.ProfileExists("Luke"));

        MapButton.interactable = false;
        EnterLastTownButton.gameObject.SetActive(false);
        if (curProfile != null)
        {
            if (curProfile.CurrentTown != null)
            {
                // Player last exited while in a Town; jump straight back into it
                EnterLastTownButton.gameObject.SetActive(true);
                EnterLastTownButton.GetComponentInChildren<TextMeshProUGUI>().text = "Enter " + curProfile.CurrentTown.DefnId;
            }
            else
                MapButton.interactable = true;
        }
    }

    public void OnDeleteAllProfilesClicked()
    {
        gameDataMgr.DeleteAllProfiles();
        updateProfileInfo();
    }

    public void OnCreateProfileClicked(string name)
    {
        ProfileName.text = "Profile " + name;
        gameDataMgr.CreateProfile(name);
        updateProfileInfo();
    }

    public void OnLoadProfileClicked(string name)
    {
        gameDataMgr.LoadProfile(name);
        updateProfileInfo();
    }

    public void OnEnterTownClicked()
    {
        SceneManager.LoadScene("TownScene", LoadSceneMode.Single);
    }

    public void OnMapClicked()
    {
        SceneManager.LoadScene("WorldMapScene", LoadSceneMode.Single);
    }
}