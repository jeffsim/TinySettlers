using System;
using TMPro;
using UnityEngine;

public class SelectBuildingToConstructEntry : MonoBehaviour
{
    SelectBuildingToConstructDialog dialog;
    public TextMeshProUGUI Name;

    [NonSerialized] BuildingDefn buildingDefn;
    [NonSerialized] SceneWithMap scene;
    [NonSerialized] Tile tile;

    internal void InitializeForBuilding(SceneWithMap scene, Tile tile, SelectBuildingToConstructDialog dialog, BuildingDefn buildingDefn)
    {
        this.dialog = dialog;
        this.buildingDefn = buildingDefn;
        this.scene = scene;
        this.tile = tile;

        Name.text = buildingDefn.FriendlyName;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnClicked()
    {
        scene.PlayerSelectedBuildingToConstructInTile(buildingDefn, tile.Data);
    }
}