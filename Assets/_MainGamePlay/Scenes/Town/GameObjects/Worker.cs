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

        var isCourier = Data.CurrentTask.Type == TaskType.FerryItem;
        CarriedItem.gameObject.SetActive(isCourier);
        if (isCourier)
            CarriedItem.text = (Data.CurrentTask as WorkerTask_FerryItem).itemBeingFerried.DefnId.Substring(0, 1);

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
