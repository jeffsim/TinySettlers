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

    void Update()
    {
        switch (dragState)
        {
            case DragState.NotDragging:
                if (Input.GetMouseButtonDown(0) && IsMouseOverThis())
                {
                    dragState = DragState.PreDrag;
                    dragStartPoint = GetMouseWorldPosition();
                    offset = transform.position - dragStartPoint;
                    if (Settings.Current.AllowFreeBuildingPlacement)
                        offset.z += .25f;
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
                {
                    dragState = DragState.Dragging;
                    if (!Settings.Current.AllowFreeBuildingPlacement)
                    {
                        draggingGO = Instantiate(scene.DraggedBuildingPrefab);
                        draggingGO.Initialize(Data.Defn, Building);
                        dragStartPoint = transform.position;
                    }
                }
                break;

            case DragState.Dragging:
                if (!Input.GetMouseButton(0))
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
                else
                {
                    if (Settings.Current.AllowFreeBuildingPlacement)
                    {
                        Vector3 mousePosition = GetMouseWorldPosition() + offset;
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            mousePosition.x = Mathf.Round(mousePosition.x / 2f) * 2f;
                            mousePosition.z = Mathf.Round(mousePosition.z / 2f) * 2f;
                        }
                        scene.Map.Town.MoveBuilding(Data, new(mousePosition.x, Settings.Current.DraggedBuildingY, mousePosition.z));
                    }
                    else
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        Plane plane = new(Vector3.up, dragStartPoint);
                        plane.Raycast(ray, out float distance);
                        Vector3 mouseIntersectPoint = ray.GetPoint(distance) + offset;
                        draggingGO.updatePosition(new Vector3(mouseIntersectPoint.x, Settings.Current.DraggedBuildingY, mouseIntersectPoint.z));
                    }
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