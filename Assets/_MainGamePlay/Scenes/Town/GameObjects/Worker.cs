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
            if (Data.CurrentTask.IsWalkingToTarget)
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
        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);

        var carriedItemRectTransform = CarriedItem.GetComponent<RectTransform>();
        switch (Data.CurrentTask.Type)
        {
            case TaskType.PickupGatherableResource:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_PickupGatherableResource).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_PickupGatherableResourceSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot:
                        carriedItemRectTransform.localPosition = itemDown;
                        carriedItemRectTransform.localScale = scaleNormal;
                        CarriedItem.color = Color.red;
                        break;
                    case WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource:
                        var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupGatherableResource.secondsToReap);
                        carriedItemRectTransform.localScale = Vector3.Lerp(scaleSmall, scaleNormal, t);
                        CarriedItem.color = Color.Lerp(Color.red, Color.white, t);
                        break;
                    case WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource:
                        var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupGatherableResource.secondsToPickup);
                        carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t2);
                        carriedItemRectTransform.localScale = scaleNormal;
                        CarriedItem.color = Color.white;
                        break;
                }
                break;

            case TaskType.DeliverItemInHandToStorageSpot:
                CarriedItem.gameObject.SetActive(true);
                CarriedItem.text = (Data.CurrentTask as WorkerTask_DeliverItemInHandToStorageSpot).GetTaskItem().Id.Substring(0, 2);
                switch ((WorkerTask_DeliverItemInHandToStorageSpotSubstate)Data.CurrentTask.substate)
                {
                    case WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo:
                        carriedItemRectTransform.localPosition = itemUp;
                        CarriedItem.color = Color.white;
                        break;
                    case WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot:
                        var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_DeliverItemInHandToStorageSpot.secondsToDrop);
                        carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t);
                        CarriedItem.color = Color.white;
                        break;
                }
                break;

            // case TaskType.FerryItem:
            //     CarriedItem.gameObject.SetActive(true);
            //     CarriedItem.text = (Data.CurrentTask as WorkerTask_FerryItem).itemBeingFerried.DefnId.Substring(0, 2);
            //     switch ((WorkerTask_FerryItemSubstate)Data.CurrentTask.substate)
            //     {
            //         case WorkerTask_FerryItemSubstate.GotoBuildingWithItem:
            //             carriedItemRectTransform.localPosition = itemDown;
            //             CarriedItem.color = Color.red;
            //             break;
            //         case WorkerTask_FerryItemSubstate.PickupItemInBuilding:
            //             var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_FerryItem.secondsToPickup);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_FerryItemSubstate.GotoDestinationBuilding:
            //             carriedItemRectTransform.localPosition = itemUp;
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_FerryItemSubstate.DropItemInBuilding:
            //             var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_FerryItem.secondsToDrop);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
            //             CarriedItem.color = Color.white;
            //             break;
            //     }
            //     break;

            // case TaskType.GatherResource:
            //     CarriedItem.gameObject.SetActive(true);
            //     CarriedItem.text = (Data.CurrentTask as WorkerTask_GatherResource).GetTaskItem().Id.Substring(0, 2);
            //     switch ((WorkerTask_GatherResourceSubstate)Data.CurrentTask.substate)
            //     {
            //         case WorkerTask_GatherResourceSubstate.GotoResourceBuilding:
            //             carriedItemRectTransform.localPosition = itemDown;
            //             CarriedItem.color = Color.red;
            //             break;
            //         case WorkerTask_GatherResourceSubstate.GatherResourceInBuilding:
            //             var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_GatherResource.secondsToGather);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_GatherResourceSubstate.ReturnToAssignedBuilding:
            //             carriedItemRectTransform.localPosition = itemUp;
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_GatherResourceSubstate.DropGatheredResource:
            //             var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_GatherResource.secondsToDrop);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
            //             CarriedItem.color = Color.white;
            //             break;
            //     }
            //     break;

            // case TaskType.PickupAbandonedItem:
            //     CarriedItem.gameObject.SetActive(true);
            //     CarriedItem.text = (Data.CurrentTask as WorkerTask_PickupAbandonedItem).GetTaskItem().Id.Substring(0, 2);
            //     switch ((WorkerTask_PickupAbandonedItemSubstate)Data.CurrentTask.substate)
            //     {
            //         case WorkerTask_PickupAbandonedItemSubstate.GotoItemOnGround:
            //             carriedItemRectTransform.localPosition = itemDown;
            //             CarriedItem.color = Color.red;
            //             break;
            //         case WorkerTask_PickupAbandonedItemSubstate.PickupItemOnGround:
            //             var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_PickupAbandonedItem.secondsToPickup);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_PickupAbandonedItemSubstate.GotoDestinationBuilding:
            //             carriedItemRectTransform.localPosition = itemUp;
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_PickupAbandonedItemSubstate.DropItemInBuilding:
            //             var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_GatherResource.secondsToDrop);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
            //             CarriedItem.color = Color.white;
            //             break;
            //     }
            //     break;

            // // GotoSpotWithResource = 0,
            // // PickupResource = 1,
            // // CarryResourceToCraftingSpot = 2,
            // // DropResourceInCraftingSpot = 3,
            // // ProduceGood = 4,
            // // CarryCraftedGoodToStorageSpot = 5, // only used if == explicit item (e.g. wood, not gold)
            // // DropCraftedGoodInStorageSpot = 6,  // only used if == explicit item (e.g. wood, not gold)

            // case TaskType.CraftGood:
            //     CarriedItem.gameObject.SetActive(true);
            //     CarriedItem.text = (Data.CurrentTask as WorkerTask_CraftItem).GetTaskItem().Id.Substring(0, 2);
            //     switch ((WorkerTask_CraftItemSubstate)Data.CurrentTask.substate)
            //     {
            //         case WorkerTask_CraftItemSubstate.GotoSpotWithResource:
            //             carriedItemRectTransform.localPosition = itemDown;
            //             CarriedItem.color = Color.red;
            //             break;
            //         case WorkerTask_CraftItemSubstate.PickupResource:
            //             var t = Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToPickup);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_CraftItemSubstate.CarryResourceToCraftingSpot:
            //             carriedItemRectTransform.localPosition = itemUp;
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_CraftItemSubstate.DropResourceInCraftingSpot:
            //             var t2 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToDrop);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t2);
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_CraftItemSubstate.ProduceGood:
            //             var t3 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToCraft);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t3);
            //             CarriedItem.color = Color.yellow;
            //             break;
            //         case WorkerTask_CraftItemSubstate.CarryCraftedGoodToStorageSpot:
            //             carriedItemRectTransform.localPosition = itemUp;
            //             CarriedItem.color = Color.white;
            //             break;
            //         case WorkerTask_CraftItemSubstate.DropCraftedGoodInStorageSpot:
            //             var t4 = Data.CurrentTask.getPercentSubstateDone(WorkerTask_CraftItem.secondsToDrop);
            //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemUp, itemDown, t4);
            //             CarriedItem.color = Color.white;
            //             break;

            //     }
            //     break;
            case TaskType.Idle:
                CarriedItem.gameObject.SetActive(true);
                carriedItemRectTransform.localPosition = itemDown;
                CarriedItem.color = Color.white;
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
}
