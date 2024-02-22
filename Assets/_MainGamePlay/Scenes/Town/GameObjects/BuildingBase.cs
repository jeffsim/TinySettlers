using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingBase : MonoBehaviour
{
    public Building Building;

    private Vector3 offset;
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

    void OnMouseDown()
    {
        if (!Settings.Current.AllowFreeBuildingPlacement)
        {
            dragState = DragState.PreDrag;
            dragStartPoint = Input.mousePosition;
        }
    }

    public void OnMouseDrag()
    {
        if (!Settings.Current.AllowFreeBuildingPlacement)
        {
            if (dragState == DragState.PreDrag)
            {
                if (Vector3.Distance(dragStartPoint, Input.mousePosition) > 10)
                {
                    dragState = DragState.Dragging;
                    draggingGO = Instantiate(scene.DraggedBuildingPrefab);
                    draggingGO.Initialize(Data.Defn, Building);
                    draggingGO.transform.position = transform.position + new Vector3(0, 10, 0);
                    dragStartPoint = transform.position;
                }
            }
            if (dragState == DragState.Dragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane plane = new(Vector3.up, dragStartPoint);
                plane.Raycast(ray, out float distance);
                Vector3 mouseIntersectPoint = ray.GetPoint(distance);

                draggingGO.updatePosition(new Vector3(mouseIntersectPoint.x, Settings.Current.DraggedBuildingY, mouseIntersectPoint.z));
            }
        }
    }

    void OnMouseUp()
    {
        if (!Settings.Current.AllowFreeBuildingPlacement)
        {
            if (dragState != DragState.Dragging)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                    scene.OnBuildingClicked(Building);
            }
            else
            {
                if (dragState == DragState.Dragging)
                    Destroy(draggingGO.gameObject);
                dragState = DragState.NotDragging;

                var validDropSpotForBuilding = scene.Map.IsValidDropSpotForBuilding(Input.mousePosition, Building);
                if (validDropSpotForBuilding)
                {
                    var tile = scene.Map.getTileAt(Input.mousePosition);
                    scene.Map.Town.MoveBuilding(Data, tile.Data.TileX, tile.Data.TileY);
                }
            }
        }
    }

    void Update()
    {
        if (!Settings.Current.AllowFreeBuildingPlacement)
            return;
        switch (dragState)
        {
            case DragState.NotDragging:
                if (Input.GetMouseButtonDown(0) && IsMouseOverThis())
                {
                    dragState = DragState.PreDrag;
                    dragStartPoint = GetMouseWorldPosition();
                    offset = transform.position - dragStartPoint;
                    offset.z += 1;
                }
                break;

            case DragState.PreDrag:
                if (!Input.GetMouseButton(0))
                {
                    dragState = DragState.NotDragging;
                    if (!EventSystem.current.IsPointerOverGameObject())
                        scene.OnBuildingClicked(Building);
                }
                else if (Vector3.Distance(dragStartPoint, GetMouseWorldPosition()) > .25f)
                    dragState = DragState.Dragging;
                break;

            case DragState.Dragging:
                if (!Input.GetMouseButton(0))
                {
                    scene.Map.Town.MoveBuilding(Data, new(Data.Location.WorldLoc.x, Settings.Current.BuildingsY, Data.Location.WorldLoc.z));
                    dragState = DragState.NotDragging;
                }
                else
                {
                    Vector3 mousePosition = GetMouseWorldPosition() + offset;
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        mousePosition.x = Mathf.Round(mousePosition.x / 2f) * 2f;
                        mousePosition.z = Mathf.Round(mousePosition.z / 2f) * 2f;
                    }
                    scene.Map.Town.MoveBuilding(Data, new(mousePosition.x, Settings.Current.DraggedBuildingY, mousePosition.z));
                }
                break;
        } 
    }

    bool IsMouseOverThis()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("BuildingBase")) && hitInfo.collider.gameObject == gameObject;
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
    }
}