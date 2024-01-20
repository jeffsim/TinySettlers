using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Needs;
    public TextMeshProUGUI Items;
    public Building building;
    public Button DestroyButton;
    public Button AssignWorkerButton;
    public Button UnassignWorkerButton;
    SceneWithMap scene;

    public BuildingDetailsItemList BuildingDetailsItemList;
    public BuildingDetailsNeedsList BuildingDetailsNeedsList;

    public void ShowForBuilding(SceneWithMap scene, Building building)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.building = building;
        Name.text = building.Data.Defn.FriendlyName + " (" + building.Data.InstanceId + ")";

        DestroyButton.interactable = building.Data.Defn.PlayerCanDestroy;

        BuildingDetailsItemList.ShowForBuilding(building);
        BuildingDetailsNeedsList.ShowForBuilding(building);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (building == null)
            return;

        // todo: store reference (or at least count) of workers in building
        // don't assign/unassign from camp, or if building can't have workers
        var assignable = building.Data.Defn.BuildingClass != BuildingClass.Camp && building.Data.Defn.HasWorkers;
        if (assignable)
        {
            var numWorkersInBuilding = scene.Map.Town.NumBuildingWorkers(building.Data);
            AssignWorkerButton.interactable = scene.Map.Town.WorkerIsAvailable() && numWorkersInBuilding < building.Data.Defn.MaxWorkers;
            UnassignWorkerButton.interactable = numWorkersInBuilding > 0;
        }
        // var str = "<color=yellow>Needs:</color>\n";
        // var needs = new List<NeedData>(building.Data.Needs);
        // str += Utilities.getNeedsDebugString(needs, false);
        // Needs.text = str;

        // if (building.Data.Defn.CanStoreItems)
        // {
        //     str = "<color=yellow>Items:</color>\n";
        //     foreach (var area in building.Data.StorageAreas)
        //         foreach (var spot in area.StorageSpots)
        //             if (spot.ItemInStorage != null)
        //                 str += spot.ItemInStorage.DefnId + "\n";
        //     Items.text = str;
        // }
        // else
        //     Items.text = "";
    }

    public void OnDestroyClicked() => scene.DestroyBuilding(this.building);
    public void OnEmptyBuildingStorage() => scene.Debug_OnEmptyBuildingStorage(this.building);

    public void OnAssignWorkerClicked()
    {
        // Assign worker from camp to this building
        // scene.Map.Town.CreateWorkerInBuilding(building.Data);
        scene.Map.Town.AssignWorkerToBuilding(building.Data);
    }

    public void OnUnassignWorkerClicked()
    {
        // Remove worker from this building and send back to camp
        scene.Map.Town.UnassignWorkerFromBuilding(building.Data);
    }
}
