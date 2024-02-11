using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectBuildingToConstructEntry : MonoBehaviour
{
    SelectBuildingToConstructDialog dialog;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI ConstructionResources;

    [NonSerialized] BuildingDefn buildingDefn;
    [NonSerialized] SceneWithMap scene;
    [NonSerialized] Tile tile;
    Vector3 worldLoc;
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    internal void InitializeForBuilding(SceneWithMap scene, Vector3 worldLoc, SelectBuildingToConstructDialog dialog, BuildingDefn buildingDefn)
    {
        this.dialog = dialog;
        this.buildingDefn = buildingDefn;
        this.scene = scene;
        this.worldLoc = worldLoc;

        Name.text = buildingDefn.FriendlyName;
        gameObject.SetActive(true);
    }

    internal void InitializeForBuilding(SceneWithMap scene, Tile tile, SelectBuildingToConstructDialog dialog, BuildingDefn buildingDefn)
    {
        this.dialog = dialog;
        this.buildingDefn = buildingDefn;
        this.scene = scene;
        this.tile = tile;

        Name.text = buildingDefn.FriendlyName;
        gameObject.SetActive(true);
    }

    public void Update()
    {
        button.interactable = scene.Map.Town.PlayerCanAffordBuilding(buildingDefn);

        // add construction resources as "Item (count)", coloring red if not enough, green if enough
        var text = "";
        foreach (var resource in buildingDefn.ResourcesNeededForConstruction)
        {
            int numInStorage = scene.Map.Town.Chart_GetNumOfItemInTown(resource.Item.Id);

            var color = numInStorage >= resource.Count ? "green" : "red";
            text += $"<color={color}>{resource.Item.FriendlyName} ({numInStorage}/{resource.Count})</color>\n";
        }
        ConstructionResources.text = text;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnClicked()
    {
        if (!scene.Map.Town.PlayerCanAffordBuilding(buildingDefn))
            return;
        if (Settings.AllowFreeBuildingPlacement)
            scene.PlayerSelectedBuildingToConstructAtWorldLoc(buildingDefn, worldLoc);
        else
            scene.PlayerSelectedBuildingToConstructInTile(buildingDefn, tile.Data);
    }
}