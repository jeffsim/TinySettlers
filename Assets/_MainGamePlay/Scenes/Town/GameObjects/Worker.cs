using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Worker : MonoBehaviour
{
    [NonSerialized] public WorkerData Data;

    public GameObject Visual;
    public GameObject Highlight;
    public TextMeshPro CarriedItem;

    public SceneWithMap scene;
    static float WorkerZ = -6.2f;

    public void Initialize(WorkerData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;

        transform.position = new Vector3(data.WorldLoc.x, data.WorldLoc.y, WorkerZ);
        updateVisual();

        Data.OnAssignedToBuilding += OnAssignedToBuilding;
    }

    void OnDestroy()
    {
        if (Data != null)
            Data.OnAssignedToBuilding -= OnAssignedToBuilding;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnWorkerClicked(this);
    }

    private void OnAssignedToBuilding()
    {
        //        Debug.Log(name);
        updateVisual();
    }

    // public void OnAssignedToBuilding(Building building)
    // {
    //     updateVisual();
    // }

    void updateVisual()
    {
        GetComponentInChildren<Renderer>().material.color = Data.AssignedBuilding.Defn.AssignedWorkerColor;
        name = "Worker - " + (Data.AssignedBuilding == null ? "none" : Data.AssignedBuilding.Defn.AssignedWorkerFriendlyName) + " (" + Data.InstanceId + ")";
    }

    public void Update()
    {
        // Data.Update();
        transform.position = new Vector3(Data.WorldLoc.x, Data.WorldLoc.y, WorkerZ);

        if (scene.Debug_DrawPaths)
            if (Data.CurrentTask.Debug_IsMovingToTarget)
            {
                // Draw path
                var offset = new Vector3(0, 0, -6);
                using (Drawing.Draw.ingame.WithColor(Color.blue))
                {
                    using (Drawing.Draw.ingame.WithLineWidth(3))
                        Drawing.Draw.ingame.Line(new Vector3(Data.WorldLoc.x, Data.WorldLoc.y, 0) + offset, Data.CurrentTask.LastMoveToTarget + offset);
                }
            }

        var itemUp = new Vector3(0, 1f, -1);
        var itemDown = new Vector3(0, 0f, -1);

        var isCourier = Data.CurrentTask.Type == TaskType.FerryItem;
        CarriedItem.gameObject.SetActive(isCourier);
        if (isCourier)
        {
            CarriedItem.text = (Data.CurrentTask as WorkerTask_FerryItem).itemBeingFerried.DefnId.Substring(0, 2);
            var rectTransform = CarriedItem.GetComponent<RectTransform>();
            switch ((WorkerTask_FerryItemSubstate)Data.CurrentTask.substate)
            {
                case WorkerTask_FerryItemSubstate.PickupItemInBuilding:
                    var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_FerryItem.secondsToPickup);
                    rectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
                    CarriedItem.color = Color.white;
                    break;
                case WorkerTask_FerryItemSubstate.DropItemInBuilding:
                    var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_FerryItem.secondsToDrop);
                    rectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
                    CarriedItem.color = Color.red;
                    break;
                case WorkerTask_FerryItemSubstate.GotoBuildingWithItem:
                    CarriedItem.color = Color.red;
                    break;
                case WorkerTask_FerryItemSubstate.GotoDestinationBuilding:
                    CarriedItem.color = Color.white;
                    break;
            }
        }
        if (Data.CurrentTask.Type == TaskType.GatherResource)
        {
            CarriedItem.gameObject.SetActive(true);
            CarriedItem.text = (Data.CurrentTask as WorkerTask_GatherResource).GetTaskItem().Id.Substring(0, 2);
            var rectTransform = CarriedItem.GetComponent<RectTransform>();
            switch ((WorkerTask_GatherResourceSubstate)Data.CurrentTask.substate)
            {
                case WorkerTask_GatherResourceSubstate.GotoResourceBuilding:
                    CarriedItem.color = Color.red;
                    break;
                case WorkerTask_GatherResourceSubstate.GatherResourceInBuilding:
                    var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_GatherResource.secondsToGather);
                    rectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
                    CarriedItem.color = Color.white;
                    break;
                case WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding:
                    CarriedItem.color = Color.white;
                    break;
                case WorkerTask_GatherResourceSubstate.DropGatheredResource:
                    var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_GatherResource.secondsToDrop);
                    rectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
                    CarriedItem.color = Color.white;
                    break;
            }
        }

        if (Data.CurrentTask.Type == TaskType.SellGood)
        {
            CarriedItem.gameObject.SetActive(true);
            CarriedItem.text = (Data.CurrentTask as WorkerTask_SellGood).GetTaskItem().Id.Substring(0, 2);
            var rectTransform = CarriedItem.GetComponent<RectTransform>();
            switch ((WorkerTask_SellGoodSubstate)Data.CurrentTask.substate)
            {
                case WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell:
                    CarriedItem.color = Color.red;
                    break;
                case WorkerTask_SellGoodSubstate.SellGood:
                    var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_SellGood.secondsToSell);
                    rectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
                    CarriedItem.color = Color.white;
                    break;
            }
        }

        // If this worker is assigned to currently selected building then highlight
        bool showHighlight = scene.BuildingDetails.isActiveAndEnabled &&
                             scene.BuildingDetails.building != null &&
                             scene.BuildingDetails.building.Data == Data.AssignedBuilding;

        // If this worker is currently selected then highlight
        if (scene.WorkerDetails.gameObject.activeSelf && scene.WorkerDetails.worker == this)
            showHighlight = true;

        Highlight.SetActive(showHighlight);
    }
}
