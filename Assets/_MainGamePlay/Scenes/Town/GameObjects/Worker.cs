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

        Data.Assignment.OnAssignedToChanged += OnAssignedToBuilding;
    }

    void OnDestroy()
    {
        if (Data != null)
            Data.Assignment.OnAssignedToChanged -= OnAssignedToBuilding;
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
        GetComponentInChildren<Renderer>().material.color = Data.Assignment.AssignedTo.Defn.AssignedWorkerColor;
        name = "Worker - " + (Data.Assignment.IsAssigned ? Data.Assignment.AssignedTo.Defn.AssignedWorkerFriendlyName + " (" + Data.InstanceId + ")" : "none");
    }

    public void Update()
    {
        // Data.Update();
        transform.position = new Vector3(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, WorkerZ);

        if (scene.Debug_DrawPaths)
            if (Data.AI.CurrentTask.IsWalkingToTarget)
            {
                // Draw path
                using (Drawing.Draw.ingame.WithColor(Color.blue))
                {
                    Vector3 loc1 = new(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, -6);
                    Vector3 loc2 = new(Data.AI.CurrentTask.LastMoveToTarget.WorldLoc.x, Data.AI.CurrentTask.LastMoveToTarget.WorldLoc.y, -6);
                    using (Drawing.Draw.ingame.WithLineWidth(3))
                        Drawing.Draw.ingame.Line(loc1, loc2);
                }
            }

        var itemUp = new Vector3(0, 1f, -1);
        var itemDown = new Vector3(0, 0f, -1);
        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);
        var percentDone = Data.AI.CurrentTask.CurSubTask.PercentDone;
        var item = Data.AI.CurrentTask.GetTaskItem();
        CarriedItem.text = item == null ? "" : item.Id.Substring(0, 2);
        CarriedItem.gameObject.SetActive(true);
        switch (Data.AI.CurrentTask.CurSubTask)
        {
            case BaseSubtask_Moving _: updateCarriedItem(itemDown, scaleNormal, Color.red); break;
            case Subtask_DropItemInItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_DropItemInMultipleItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromItemSpot _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromGround _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_ReapItem _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_SellItemInHands _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_CraftItem _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_Wait _: updateCarriedItem(itemDown, scaleNormal, Color.Lerp(Color.green, Color.white, percentDone)); break;
            default:
                // Debug.Assert(false, "Unhandled subtask " + Data.AI.CurrentTask.CurSubTask);
                break;
        }
        /*
        switch (Data.AI.CurrentTask.Type)
        {
            case TaskType.SellItem:
                switch (Data.AI.CurrentTask.SubtaskIndex)
                {
                    case 0://WorkerTask_SellItemSubstate.GotoItemToSell:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case 1://WorkerTask_SellItemSubstate.PickupItemToSell:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white);
                        break;
                    case 2:// WorkerTask_SellItemSubstate.SellItem:
                        updateCarriedItem(itemUp, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.red, Color.white, percentDone));
                        break;
                }
                break;

            case TaskType.GetGatherableResource:
                switch (Data.AI.CurrentTask.SubtaskIndex)
                {
                    case 0: // WorkerTask_PickupGatherableResourceSubstate.GotoGatheringSpot:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case 1: // WorkerTask_PickupGatherableResourceSubstate.ReapGatherableResource:
                        updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.red, Color.white, percentDone));
                        break;
                    case 2: // WorkerTask_PickupGatherableResourceSubstate.PickupGatherableResource:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.PickupItemFromGround:
                switch (Data.AI.CurrentTask.SubtaskIndex)
                {
                    case 0:// WorkerTask_PickupAbandonedItemFromGroundSubstate.GotoItemOnGround:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case 1: //WorkerTask_PickupAbandonedItemFromGroundSubstate.PickupItemFromGround:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.PickupItemInStorageSpot:
                switch (Data.AI.CurrentTask.SubtaskIndex)
                {
                    case 0: //WorkerTask_PickupItemFromStorageSpotSubstate.GotoItemSpotWithItem:
                        updateCarriedItem(itemDown, scaleNormal, Color.red);
                        break;
                    case 1: //WorkerTask_PickupItemFromStorageSpotSubstate.PickupItemFromItemSpot:
                        updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.DeliverItemInHandToStorageSpot:
                switch (Data.AI.CurrentTask.SubtaskIndex)
                {
                    case 0: //WorkerTask_DeliverItemInHandToStorageSpotSubstate.GotoStorageSpotToDeliverItemTo:
                        updateCarriedItem(itemUp, scaleNormal, Color.white);
                        break;
                    case 1: //WorkerTask_DeliverItemInHandToStorageSpotSubstate.DropItemInDestinationStorageSpot:
                        updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white);
                        break;
                }
                break;

            case TaskType.CraftGood:
                switch (Data.AI.CurrentTask.CurSubTask)
                {
                    case WorkerSubtask_WalkToItemSpot _: updateCarriedItem(itemDown, scaleNormal, Color.red); break;
                    case WorkerSubtask_WalkToMultipleItemSpot _: updateCarriedItem(itemDown, scaleNormal, Color.red); break;
                    case WorkerSubtask_DropItemInItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
                    case WorkerSubtask_DropItemInMultipleItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
                    case WorkerSubtask_PickupItemFromBuilding _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
                    case WorkerSubtask_CraftItem _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
                    default:
                        Debug.Assert(false, "Unhandled subtask " + Data.AI.CurrentTask.CurSubTask);
                        break;
                }
                break;

            case TaskType.Idle:
                updateCarriedItem(itemDown, scaleNormal, Color.white);
                CarriedItem.text = "<i>i</i>";
                break;

                // case TaskType.SellGood:
                //     CarriedItem.gameObject.SetActive(true);

                //     switch ((WorkerTask_SellGoodSubstate)Data.AI.CurrentTask.substate)
                //     {
                //         case WorkerTask_SellGoodSubstate.GotoSpotWithGoodToSell:
                //             CarriedItem.text = (Data.AI.CurrentTask as WorkerTask_SellGood).GetTaskItem().Id.Substring(0, 2);
                //             carriedItemRectTransform.localPosition = itemDown;
                //             CarriedItem.color = Color.red;
                //             break;
                //         case WorkerTask_SellGoodSubstate.SellGood:
                //             var sellPrice = (Data.AI.CurrentTask as WorkerTask_SellGood).GetTaskItem().BaseSellPrice; // TODO: Multipliers
                //             CarriedItem.text = sellPrice + "gp";
                //             var t = Data.AI.CurrentTask.getPercentSubstateDone(WorkerTask_SellGood.secondsToSell);
                //             carriedItemRectTransform.localPosition = Vector3.Lerp(itemDown, itemUp, t);
                //             CarriedItem.color = Color.white;
                //             break;
                //     }
                //     break;
        }
*/
        // If this worker is assigned to currently selected building then highlight
        bool showHighlight = scene.BuildingDetails.isActiveAndEnabled &&
                             scene.BuildingDetails.building != null &&
                             scene.BuildingDetails.building.Data == Data.Assignment.AssignedTo;

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
