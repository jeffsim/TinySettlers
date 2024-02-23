using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Worker : MonoBehaviour
{
    [NonSerialized] public WorkerData Data;
    public GameObject Visual;
    public GameObject Highlight;
    public Item ItemVisual;
    public GameObject CarriedItem;
    public SceneWithMap scene;
    float lineY = 0f;
    Vector3 ZFightingOffset;
    public Animator animator;
    public GameObject CarryAboveHeadSpot;

    public void Initialize(WorkerData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        Debug.Assert(CarryAboveHeadSpot != null, "CarryAboveHeadSpot not found " + Data);
        ZFightingOffset = scene.Map.Town.TownWorkerMgr.Workers.IndexOf(Data) * new Vector3(0, 0.001f, 0);
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
        updateVisual();
    }

    void updateVisual()
    {
        // Instantiate VisualPrefab from Defn and attach
        Visual = Instantiate(Data.Defn.VisualPrefab);
        Visual.transform.SetParent(transform, false);
        animator = Visual.GetComponent<Animator>();
        animator.Play("idle_01");
        // Find CarryAboveHeadSpot
        CarryAboveHeadSpot = Visual.transform.GetComponentInChildren<CarrySpot>().gameObject;

        // GetComponentInChildren<Renderer>().material.color = Data.Assignment.AssignedTo.Defn.AssignedWorkerColor;
        name = "Worker - " + (Data.Assignment.IsAssigned ? Data.Assignment.AssignedTo.Defn.AssignedWorkerFriendlyName + " (" + Data.InstanceId + ")" : "none");
    }

    bool startedPlayingWalkAnim;

    public void Update()
    {
        transform.position = Data.Location.WorldLoc + ZFightingOffset;

        // Face the direction we are moving, but always along the Z plane
        if (Data.AI.CurrentTask.IsWalkingToTarget)
        {
            if (!startedPlayingWalkAnim)
            {
                animator.Play("run 0");
                startedPlayingWalkAnim = true;
            }

            var target = Data.AI.CurrentTask.LastMoveToTarget.WorldLoc;
            var direction = target - Data.Location.WorldLoc;
            direction.y = 0;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }
        else if (startedPlayingWalkAnim)
        {
            animator.Play("idle_01");
            startedPlayingWalkAnim = false;
        }

        // CHeck if picked up/dropped item; update Item visual appropriately
        ItemData itemToShow = null;
        switch (Data.AI.CurrentTask.CurSubTask)
        {
            case BaseSubtask_Moving _: itemToShow = Data.Hands.HasItem ? Data.Hands.Item : null; break;
            case Subtask_DropItemInItemSpot _: itemToShow = Data.Hands.Item; break;
            case Subtask_DropItemInMultipleItemSpot _: itemToShow = Data.Hands.Item; break;
            case Subtask_PickupItemFromItemSpot _: itemToShow = Data.AI.CurrentTask.GetTaskItem(); break;
            case Subtask_PickupItemFromGround _: itemToShow = Data.AI.CurrentTask.GetTaskItem(); break;
            case Subtask_ReapItem _: itemToShow = Data.AI.CurrentTask.GetTaskItem(); break;
            case Subtask_SellItemInHands _: itemToShow = Data.Hands.Item; break;
            case Subtask_CraftItem _: itemToShow = Data.AI.CurrentTask.GetTaskItem(); break;
            case Subtask_Wait _: itemToShow = Data.Hands.Item; break;

            default:
                // Debug.Assert(false, "Unhandled subtask " + Data.AI.CurrentTask.CurSubTask);
                break;
        }

        if ((itemToShow != null && ItemVisual == null) ||
            (itemToShow != null && ItemVisual != null && ItemVisual.Data != itemToShow))
        {
            if (ItemVisual != null)
                Destroy(ItemVisual.gameObject);

            ItemVisual = Instantiate(itemToShow.Defn.VisualPrefab);
            ItemVisual.transform.SetParent(CarryAboveHeadSpot.transform, false);
            ItemVisual.GetComponent<Item>().Initialize(itemToShow, scene);
        }
        else if (itemToShow == null && ItemVisual != null)
        {
            Destroy(ItemVisual.gameObject);
            ItemVisual = null;
        }

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

        var itemUp = new Vector3(0, Settings.Current.ItemCarryY, 0);
        var itemDown = new Vector3(0, Settings.Current.ItemDropY, 0);
        var scaleSmall = new Vector3(0, 0, 0);
        var scaleNormal = new Vector3(1, 1, 1);
        var percentDone = Data.AI.CurrentTask.CurSubTask.PercentDone;
        // CarriedItem.text = item == null ? "" : item.Id.Substring(0, 2);
        // CarriedItem.gameObject.SetActive(true);
        switch (Data.AI.CurrentTask.CurSubTask)
        {
            case BaseSubtask_Moving _: updateCarriedItem(Data.Hands.HasItem ? itemUp : itemDown, scaleNormal, Data.Hands.HasItem ? Color.white : Color.red); break;
            case Subtask_DropItemInItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_DropItemInMultipleItemSpot _: updateCarriedItem(Vector3.Lerp(itemUp, itemDown, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromItemSpot _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_PickupItemFromGround _: updateCarriedItem(Vector3.Lerp(itemDown, itemUp, percentDone), scaleNormal, Color.white); break;
            case Subtask_ReapItem _: updateCarriedItem(itemDown, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_SellItemInHands _: updateCarriedItem(itemUp, Vector3.Lerp(scaleNormal, scaleSmall, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_CraftItem _: updateCarriedItem(itemUp, Vector3.Lerp(scaleSmall, scaleNormal, percentDone), Color.Lerp(Color.green, Color.white, percentDone)); break;
            case Subtask_Wait _: /*CarriedItem.text = "<i>I</i>"; */updateCarriedItem(itemDown, scaleNormal, Color.Lerp(Color.green, Color.white, percentDone)); break;
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
        if (ItemVisual != null)
        {
            ItemVisual.transform.localPosition = position;
            ItemVisual.transform.localScale = scale;
        }
        // carriedItemRectTransform.localPosition = position;
        // carriedItemRectTransform.localScale = scale;
        // CarriedItem.color = color;
    }
}
