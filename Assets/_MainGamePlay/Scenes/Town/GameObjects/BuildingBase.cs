using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingBase : MonoBehaviour
{
    public Building Building;

    //  private Vector3 offset;
    public SceneWithMap scene;
    [NonSerialized] public BuildingData Data;

    // Dragging properties
    enum DragState { NotDragging, PreDrag, Dragging };
    DragState dragState;
    Vector3 dragStartPoint;

    public void InitializeForBuilding(Building building, SceneWithMap scene, BuildingData data)
    {
        this.scene = scene;
        Data = data;
        Building = building;
    }

    void Update()
    {
        switch (dragState)
        {
            case DragState.NotDragging:
                if (Input.GetMouseButtonDown(0) && IsMouseOverThis())
                    StartPreDrag();
                break;

            case DragState.PreDrag:
                if (!Input.GetMouseButton(0))
                    CancelPreDrag();
                else if (Vector3.Distance(dragStartPoint, Utilities.GetMouseWorldPosition()) > .25f)
                    StartDragging();
                break;

            case DragState.Dragging:
                if (!Input.GetMouseButton(0))
                    StopDragging();
                else
                    DragBuilding();
                break;
        }
    }


    private void StartPreDrag()
    {
        dragState = DragState.PreDrag;
        dragStartPoint = Utilities.GetMouseWorldPosition();
    }

    private void CancelPreDrag()
    {
        dragState = DragState.NotDragging;
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnBuildingClicked(Building);
    }

    private void StartDragging()
    {
        dragState = DragState.Dragging;
    }

    private void DragBuilding()
    {
        Vector3 worldPos = Utilities.GetMouseWorldPosition();
        var hexTileWorldPos = Utilities.GetCenterOfHexTileClosestToWorldPos(worldPos);
        var hexTile = Utilities.ConvertWorldPosToHexTile(hexTileWorldPos);

        scene.Map.Town.MoveBuilding(Data, hexTile);
    }

    private void StopDragging()
    {
        Vector3 worldPos = Utilities.GetMouseWorldPosition();
        var hexTileWorldPos = Utilities.GetCenterOfHexTileClosestToWorldPos(worldPos);
        var hexTile = Utilities.ConvertWorldPosToHexTile(hexTileWorldPos);
        scene.Map.Town.MoveBuilding(Data, hexTile);
        dragState = DragState.NotDragging;
    }

    bool IsMouseOverThis()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("BuildingBase")) && hitInfo.collider.gameObject == gameObject;
    }
}