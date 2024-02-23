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
    public Button TogglePauseButton;
    public Button EmptyStorageButton;
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
        TogglePauseButton.interactable = building.Data.Defn.PlayerCanPause;
        EmptyStorageButton.interactable = building.Data.Defn.CanStoreItems;
        AssignWorkerButton.interactable = building.Data.Defn.HasWorkers;
        UnassignWorkerButton.interactable = building.Data.Defn.HasWorkers;
        BuildingDetailsItemList.ShowForBuilding(building);
        BuildingDetailsNeedsList.ShowForBuilding(building);
    }

    public void OnDebugAddWorkerClicked()
    {
        scene.Map.Town.CreateWorkerInBuilding(building.Data);
    }

    public void OnBuildingPauseToggleClicked()
    {
        building.Data.Pausable.TogglePaused();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (building == null)
            return;

        var buildingData = building.Data;
        var buildingDefn = buildingData.Defn;
        var townWorkerMgr = scene.Map.Town.TownWorkerMgr;

        if (buildingData.Occupiable != null)
            Name.text = buildingDefn.FriendlyName + " (" + buildingData.InstanceId + ") (w:" + buildingData.Occupiable.NumOccupants + "/" + buildingData.Occupiable.MaxOccupants + ")";

        // todo: store reference (or at least count) of workers in building
        // don't assign/unassign from camp, or if building can't have workers
        var assignable = buildingDefn.BuildingClass != BuildingClass.Camp && buildingDefn.HasWorkers;
        if (assignable)
        {
            var numWorkersInBuilding = townWorkerMgr.NumBuildingWorkers(building.Data);
            AssignWorkerButton.interactable = townWorkerMgr.WorkerIsAvailable() && numWorkersInBuilding < buildingDefn.MaxWorkers;
            UnassignWorkerButton.interactable = numWorkersInBuilding > 0 && buildingData != scene.Map.Town.Camp;
        }
    }

    public void OnDestroyClicked() => scene.DestroyBuilding(this.building);
    public void OnEmptyBuildingStorage() => scene.Debug_OnEmptyBuildingStorage(this.building);

    public void OnAssignWorkerClicked()
    {
        // Assign worker from camp to this building
        scene.Map.Town.AssignWorkerToBuilding(building.Data);
    }

    public void OnUnassignWorkerClicked()
    {
        // Remove worker from this building and send back to camp
        scene.Map.Town.UnassignWorkerFromBuilding(building.Data);
    }
}
