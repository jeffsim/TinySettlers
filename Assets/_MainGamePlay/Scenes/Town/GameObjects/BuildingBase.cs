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
    DraggedBuilding draggingGO;
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
        if (!Settings.Current.AllowFreeBuildingPlacement)
        {
            draggingGO = Instantiate(scene.DraggedBuildingPrefab);
            draggingGO.Initialize(Data.Defn, Building);
            dragStartPoint = transform.position;
        }
    }

    private void DragBuilding()
    {
        Vector3 worldPos = Utilities.GetMouseWorldPosition();
        if (Settings.Current.HexTiles)
            scene.Map.Town.MoveBuilding(Data, Utilities.GetCenterOfHexTileClosestToWorldPos(worldPos));
        else if (Settings.Current.AllowFreeBuildingPlacement)
            scene.Map.Town.MoveBuilding(Data, new(worldPos.x, Settings.Current.DraggedBuildingY, worldPos.z));
        else
            draggingGO.updatePosition(worldPos);
    }

    private void StopDragging()
    {
        scene.Map.Town.MoveBuilding(Data, new(Data.Location.WorldLoc.x, Settings.Current.BuildingsY, Data.Location.WorldLoc.z));
        dragState = DragState.NotDragging;
        if (!Settings.Current.AllowFreeBuildingPlacement)
        {
            Destroy(draggingGO.gameObject);
            var validDropSpotForBuilding = scene.Map.IsValidDropSpotForBuilding(Input.mousePosition, Building);
            if (validDropSpotForBuilding)
            {
                var tile = scene.Map.getTileAt(Input.mousePosition);
                scene.Map.Town.MoveBuilding(Data, tile.Data.TileX, tile.Data.TileY);
            }
        }
    }

    bool IsMouseOverThis()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("BuildingBase")) && hitInfo.collider.gameObject == gameObject;
    }
}