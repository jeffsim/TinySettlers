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
    RectTransform carriedItemRectTransform;
    
    public void Initialize(WorkerData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        carriedItemRectTransform = CarriedItem.GetComponent<RectTransform>();

        transform.position = data.Location.WorldLoc;
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

    float lineY = 3f;
    public void Update()
    {
        transform.position = Data.Location.WorldLoc;

        if (scene.Debug_DrawPaths)
        {
            if (Data.AI.CurrentTask.IsWalkingToTarget)
            {
                // Draw path
                using (Drawing.Draw.ingame.WithColor(Color.blue))
                {
                    Vector3 loc1 = new(Data.Location.WorldLoc.x, lineY, Data.Location.WorldLoc.z);
                    Vector3 loc2 = new(Data.AI.CurrentTask.LastMoveToTarget.WorldLoc.x, lineY, Data.AI.CurrentTask.LastMoveToTarget.WorldLoc.z);
                    using (Drawing.Draw.ingame.WithLineWidth(3))
                        Drawing.Draw.ingame.Line(loc1, loc2);
                }
            }
            // Draw line to worker's reserved slots if the worker's detail dialog is currently open
            if (scene.WorkerDetails.gameObject.activeSelf && scene.WorkerDetails.worker == this)
            {
                var reservedSpots = Data.AI.CurrentTask.ReservedSpots;
                foreach (var reservedSpot in reservedSpots)
                {
                    if (reservedSpot is StorageSpotData spot)
                    {
                        using (Drawing.Draw.ingame.WithColor(Color.red))
                        {
                            Vector3 loc1 = new(Data.Location.WorldLoc.x, lineY, Data.Location.WorldLoc.z);
                            Vector3 loc2 = new(spot.Location.WorldLoc.x, lineY, spot.Location.WorldLoc.z);
                            using (Drawing.Draw.ingame.WithLineWidth(1))
                                Drawing.Draw.ingame.Line(loc1, loc2);
                            using (Drawing.Draw.ingame.WithLineWidth(3))
                                Drawing.Draw.ingame.xy.Circle(new Vector3(spot.Location.WorldLoc.x, lineY, spot.Location.WorldLoc.z), .125f);
                        }
                    }
                }
            }
        }

        var itemUp = new Vector3(0, .5f, 1);
        var itemDown = new Vector3(0, .5f, 0);
        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);
        var percentDone = Data.AI.CurrentTask.CurSubTask.PercentDone;
        var item = Data.AI.CurrentTask.GetTaskItem();
        CarriedItem.text = item == null ? "" : item.Id.Substring(0, 2);
        CarriedItem.gameObject.SetActive(true);
        switch (Data.AI.CurrentTask.CurSubTask)
        {
            case BaseSubtask_Moving _: updateCarriedItem(Data.Hands.HasItem ? itemUp : itemDown, scaleNormal, Data.Hands.HasItem ? Color.white : Color.red); break;
            case Subtask_DropItemInItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_DropItemInMultipleItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromItemSpot _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromGround _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_ReapItem _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_SellItemInHands _: updateCarriedItem(itemUp, Vector3.Lerp(scaleNormal, scaleSmall, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_CraftItem _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_Wait _: CarriedItem.text = "<i>I</i>"; updateCarriedItem(itemDown, scaleNormal, Color.Lerp(Color.green, Color.white, percentDone)); break;
            default:
                // Debug.Assert(false, "Unhandled subtask " + Data.AI.CurrentTask.CurSubTask);
                break;
        }

        // If this worker is assigned to currently selected building then highlight
        bool showHighlight = false;
        var selectedBuilding = scene.BuildingDetails.building;
        if (scene.BuildingDetails.isActiveAndEnabled && selectedBuilding != null)
        {
            showHighlight |= selectedBuilding.Data == Data.Assignment.AssignedTo;
            showHighlight |= selectedBuilding.Data.Defn.WorkersCanLiveHere && selectedBuilding.Data.OccupantMgr.IsOccupant(Data);
        }

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
