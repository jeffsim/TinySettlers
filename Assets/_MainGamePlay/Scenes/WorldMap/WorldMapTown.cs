using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldMapTown : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public Button Button;
    WorldMapScene scene;
    [SerializeReference] TownData town;

    internal void Initialize(TownData town, WorldMapScene scene)
    {
        this.scene = scene;
        this.town = town;
        transform.position = new Vector3(town.WorldX, town.WorldY, 0);
        Button.interactable = town.CanEnter;
        Name.text = town.Defn.FriendlyName;
    }

    public void OnClicked()
    {
        scene.TownClicked(town);
    }
}