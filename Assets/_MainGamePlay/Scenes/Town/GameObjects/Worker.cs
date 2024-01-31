using System;
using System.Collections;
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
    RectTransform carriedItemRectTransform;
    public void Initialize(WorkerData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        carriedItemRectTransform = CarriedItem.GetComponent<RectTransform>();

        transform.position = new Vector3(data.Location.WorldLoc.x, data.Location.WorldLoc.y, WorkerZ);
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
        transform.position = new Vector3(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, WorkerZ);

        if (scene.Debug_DrawPaths)
            if (Data.CurrentTask.IsWalkingToTarget)
            {
                // Draw path
                var offset = new Vector3(0, 0, -6);
                using (Drawing.Draw.ingame.WithColor(Color.blue))
                {
                    using (Drawing.Draw.ingame.WithLineWidth(3))
                        Drawing.Draw.ingame.Line(new Vector3(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, 0) + offset, Data.CurrentTask.LastMoveToTarget.WorldLoc + offset);
                }
            }

        var itemUp = new Vector3(0, 1f, -1);
        var itemDown = new Vector3(0, 0f, -1);
        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);

        switch (Data.CurrentTask.Type)
        {
            case TaskType.SellItem:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_SellItem).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_SellItemSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_SellItemSubstate.GotoItemToSell:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case WorkerTask_SellItemSubstate.PickupItemToSell:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_SellItem.secondsToPickup)), scaleNormal, Color.white);
                        break;
                    case WorkerTask_SellItemSubstate.SellItem:
                        var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_SellItem.secondsToSell);
                        updateCarriedItem(itemUp, Vector3.Lerp(scaleSmall, scaleNormal, t), Color.Lerp(Color.red, Color.white, t));
                        break;
                }
                break;

            case TaskType.PickupGatherableResource:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_PickupGatherableResource).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_PickupGatherableResourceSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource:
                        var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupGatherableResource.secondsToReap);
                        updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, t), Color.Lerp(Color.red, Color.white, t));
                        break;
                    case WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupGatherableResource.secondsToPickup)), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.PickupItemInStorageSpot:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_PickupItemFromStorageSpot).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_PickupItemFromStorageSpotSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupItemFromStorageSpot.secondsToPickup)), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.PickupItemFromGround:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_PickupAbandonedItemFromGround).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_PickupAbandonedItemFromGroundSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_PickupAbandonedItemFromGroundSubstate.GotoItemOnGround:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case WorkerTask_PickupAbandonedItemFromGroundSubstate.PickupItemFromGround:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupAbandonedItemFromGround.secondsToPickup)), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.DeliverItemInHandToStorageSpot:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_DeliverItemInHandToStorageSpot).GetTaskItem().Id[..2];
                switch ((WorkerTask_DeliverItemInHandToStorageSpotSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo:
                        updateCarriedItem(itemUp, scaleNormal, Color.white);
                        break;
                    case WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot:
                        updateCarriedItem(Vector3.Lerp(itemUp, itemDown, Data.CurrentTask.getPercentSubstateDone(WorkerTask_DeliverItemInHandToStorageSpot.secondsToDrop)), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.CraftGood:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_CraftItem).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_CraftItemSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_CraftItemSubstate.GotoSpotWithResource:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case WorkerTask_CraftItemSubstate.PickupResource:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToPickupSourceResource)), scaleNormal, Color.white);
                        break;
                    case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
                        updateCarriedItem(itemUp, scaleNormal, Color.white);
                        break;
                    case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
                        updateCarriedItem(Vector3.Lerp(itemUp, itemDown, Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToDropSourceResource)), scaleNormal, Color.white);
                        break;
                    case WorkerTask_CraftItemSubstate.CraftGood:
                        var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToCraft);
                        updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, t), Color.Lerp(Color.green, Color.white, t));
                        break;
                    case WorkerTask_CraftItemSubstate.PickupProducedGood:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToPickupCraftedGood)), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.Idle:
                CarriedItem.gameObject.SetActive(true);
                updateCarriedItem(itemDown, scaleNormal, Color.white);
                CarriedItem.text = "<i>i</i>";
                break;

                // case TaskType.SellGood:
                //     CarriedItem.gameObject.SetActive(true);

                //     switch ((WorkerTask_SellGoodSubstate)Data.CurrentTask.substate)
                //     {
                //         case WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell:
                //             CarriedItem.text = (Data.CurrentTask as WorkerTask_SellGood).GetTaskItem().Id.Substring(0, 2);
                //             carriedItemRectTransform.localPosition = itemDown;
                //             CarriedItem.color = Color.red;
                //             break;
                //         case WorkerTask_SellGoodSubstate.SellGood:
                //             var sellPrice = (Data.CurrentTask as WorkerTask_SellGood).GetTaskItem().BaseSellPrice; // TODO: Multipliers
                //             CarriedItem.text = sellPrice + "gp";
                //             var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_SellGood.secondsToSell);
                //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
                //             CarriedItem.color = Color.white;
                //             break;
                //     }
                //     break;
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

    private void updateCarriedItem(Vector3 position, Vector3 scale, Color color)
    {
        carriedItemRectTransform.localPosition = position;
        carriedItemRectTransform.localScale = scale;
        CarriedItem.color = color;
    }
}
