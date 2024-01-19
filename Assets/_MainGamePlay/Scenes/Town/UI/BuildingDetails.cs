using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BuildingDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Needs;
    public TextMeshProUGUI Items;
    public Building building;
    SceneWithMap scene;

    public void ShowForBuilding(SceneWithMap scene, Building building)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.building = building;
        Name.text = building.Data.Defn.FriendlyName + " (" + building.Data.InstanceId + ")";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    void Update()
    {
        if (building == null)
            return;

        var str = "<color=yellow>Needs:</color>\n";
        var needs = new List<NeedData>(building.Data.Needs);
        str += Utilities.getNeedsDebugString(needs, false);
        Needs.text = str;

        if (building.Data.Defn.CanStoreItems)
        {
            str = "<color=yellow>Items:</color>\n";
            foreach (var area in building.Data.StorageAreas)
                foreach (var spot in area.StorageSpots)
                    if (spot.ItemInStorage != null)
                        str += spot.ItemInStorage.DefnId + "\n";
            Items.text = str;
        }
    }

    public void OnDestroyClicked() => scene.DestroyBuilding(this.building);
    public void OnEmptyBuildingStorage() => scene.Debug_OnEmptyBuildingStorage(this.building);
}
