using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapTown : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public Button Button;
    WorldMapScene scene;
    [SerializeReference] World_TownData town;

    internal void Initialize(World_TownData town, WorldMapScene scene, TownData currentTown)
    {
        this.scene = scene;
        this.town = town;
        var townDefn = GameDefns.Instance.TownDefns[town.TownDefnId];
        transform.position = new Vector3(townDefn.WorldX, townDefn.WorldY, 0);

        Button.interactable = town.CanEnter;

        // New: only allow reentering the last played town.
        if (currentTown != null && currentTown.DefnId != town.TownDefnId)
            Button.interactable = false;

        Name.text = townDefn.FriendlyName;
        switch (town.State)
        {
            case TownState.Undiscovered: Name.text += " (U)"; break;
            case TownState.Available: Name.text += " (A)"; break;
            case TownState.InProgress: Name.text += " (I)"; break;
            case TownState.Completed: Name.text += " (C)"; break;
        }
    }

    public void OnClicked()
    {
        scene.TownClicked(town);
    }
}