using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Building : MonoBehaviour
{
    [NonSerialized] public BuildingData Data;

    public TextMeshPro Name;
    public GameObject StorageEditorFolder;
    public GameObject StorageFullIndicator;
    public GameObject PausedIndicator;
    public GameObject Visual;
    public GameObject Background;
    public GameObject Bottom;
    public SceneWithMap scene;

    private Vector3 offset;

    // Dragging properties
    DraggedBuilding draggingGO;
    enum DragState { NotDragging, PreDrag, Dragging };
    DragState dragState;
    Vector3 dragStartPoint;

    public void Initialize(BuildingData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;

        name = data.DefnId + " " + data.InstanceId;

        Data.OnLocationChanged += OnLocationChanged;

        Name.text = data.Defn.FriendlyName;
        Background.GetComponent<Renderer>().material.color = data.Defn.BuildingColor;
        var color = data.Defn.BuildingColor / 2f;
        Bottom.GetComponent<Renderer>().material.color = color;
        transform.position = data.Location.WorldLoc;

        if (Data.Defn.CanStoreItems)
            for (int i = 0; i < Data.Defn.StorageAreas.Count; i++)
            {
                var item = Instantiate(scene.BuildingStorageAreaPrefab);
                item.transform.SetParent(StorageEditorFolder.transform, false);
                item.Initialize(Data.StorageAreas[i], Data.Defn.StorageAreas[i], this, scene.BuildingStoragePilePrefab, StorageEditorFolder.transform);
            }

        if (Data.Defn.ResourcesCanBeGatheredFromHere)
            for (int i = 0; i < Data.Defn.GatheringSpots.Count; i++)
            {
                var spot = Instantiate(scene.GatheringSpotPrefab);
                spot.transform.SetParent(transform, false);
                spot.Initialize(scene, Data.GatheringSpots[i], i, this);
            }

        if (Data.Defn.CanCraft)
            for (int i = 0; i < Data.Defn.CraftingSpots.Count; i++)
            {
                var spot = Instantiate(scene.CraftingSpotPrefab);
                spot.transform.SetParent(transform, false);
                spot.Initialize(Data.CraftingSpots[i], i, this, scene);
            }

        if (Data.Defn.WorkersCanRestHere)
            for (int i = 0; i < Data.Defn.SleepingSpots.Count; i++)
            {
                var spot = Instantiate(scene.SleepingSpotPrefab);
                spot.transform.SetParent(transform, false);
                spot.Initialize(scene, Data.SleepingSpots[i], i, this);
            }

        if (Data.Defn.VisualPrefab != null)
        {
            var buildingVisual = Instantiate(Data.Defn.VisualPrefab);
            buildingVisual.transform.SetParent(Visual.transform, false);
            buildingVisual.transform.localPosition = Data.Defn.VisualOffset;
            buildingVisual.transform.localScale = Data.Defn.VisualScale;
            buildingVisual.transform.localRotation = Data.Defn.VisualRotation;
        }
    }

    void OnDestroy()
    {
        if (Data != null)
            Data.OnLocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged()
    {
        // if (Settings.AllowFreeBuildingPlacement)
        //     transform.position = new(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, -6);
        // else
        //     transform.position = Data.Location.WorldLoc;
        transform.position = Data.Location.WorldLoc;
    }

    void OnMouseDown()
    {
        if (Settings.Current.AllowFreeBuildingPlacement)
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragState = DragState.PreDrag;
                dragStartPoint = GetMouseWorldPosition();
                offset = transform.position - dragStartPoint;
            }
        }
        else
        {
            if (!Data.Defn.PlayerCanMove)
                return;
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
                    draggingGO = GameObject.Instantiate(scene.DraggedBuildingPrefab);
                    draggingGO.Initialize(Data.Defn, this);
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
        if (Settings.Current.AllowFreeBuildingPlacement)
        {
            if (dragState == DragState.Dragging)
            {
                dragState = DragState.NotDragging;
                Data.Location.WorldLoc = transform.position;
                putBuildingOnTopOfOthers();
            }
            else
            {
                dragState = DragState.NotDragging;
                if (!EventSystem.current.IsPointerOverGameObject())
                    scene.OnBuildingClicked(this);
            }
        }
        else
        {
            if (dragState != DragState.Dragging)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                    scene.OnBuildingClicked(this);
            }
            else
            {
                if (dragState == DragState.Dragging)
                    Destroy(draggingGO.gameObject);
                dragState = DragState.NotDragging;

                var validDropSpotForBuilding = scene.Map.IsValidDropSpotForBuilding(Input.mousePosition, this);
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
        StorageFullIndicator.SetActive(Data.Defn.CanStoreItems && Data.IsStorageFull);
        PausedIndicator.SetActive(Data.IsPaused);
        if (Settings.Current.AllowFreeBuildingPlacement)
        {
            if (dragState == DragState.PreDrag)
            {
                Vector3 mousePosition = GetMouseWorldPosition();
                if (Vector3.Distance(dragStartPoint, mousePosition) > .25f)
                {
                    dragState = DragState.Dragging;
                    putBuildingOnTopOfOthers();
                }
            }
            else if (dragState == DragState.Dragging)
            {
                Vector3 mousePosition = GetMouseWorldPosition() + offset;
                // snap to grid if shift is pressed
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    mousePosition.x = Mathf.Round(mousePosition.x / 2f) * 2f;
                    mousePosition.y = Mathf.Round(mousePosition.y / .5f) * .5f;
                }
                scene.Map.Town.MoveBuilding(Data, new(mousePosition.x, transform.position.z, mousePosition.y));
            }
        }
    }

    private void putBuildingOnTopOfOthers()
    {
        var buildings = scene.Map.GetBuildingGOs();

        // sort by y
        buildings.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        buildings.Remove(this);
        buildings.Insert(0, this);
        for (int i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            float yPosition = i * .62f - 2.5f;
            if (building == this && dragState == DragState.Dragging)
                yPosition = -95;
            building.transform.position = new(building.transform.position.x, yPosition, building.transform.position.z);
            building.Data.Location.WorldLoc = building.transform.position;
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        // Convert the mouse screen position to a world position on the same z-axis as the node
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.y = Camera.main.WorldToScreenPoint(transform.position).y;
        return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
    }
}