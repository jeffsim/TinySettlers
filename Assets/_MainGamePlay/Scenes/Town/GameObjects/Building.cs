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
    public SceneWithMap scene;

    private Vector3 offset;

    // Dragging properties
    // DraggedBuilding draggingGO;
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
        GetComponentInChildren<Renderer>().material.color = data.Defn.BuildingColor;
        transform.position = new Vector3(data.Location.WorldLoc.x, data.Location.WorldLoc.y, 0);

        for (int i = 0; i < Data.Defn.StorageAreas.Count; i++)
        {
            var item = Instantiate(scene.BuildingStorageAreaPrefab);
            item.transform.SetParent(StorageEditorFolder.transform, false);
            item.Initialize(Data.StorageAreas[i], Data.Defn.StorageAreas[i], this, scene.BuildingStoragePilePrefab, StorageEditorFolder.transform);
        }

        for (int i = 0; i < Data.Defn.GatheringSpots.Count; i++)
        {
            var spot = Instantiate(scene.GatheringSpotPrefab);
            spot.transform.SetParent(transform, false);
            spot.Initialize(scene, Data.GatheringSpots[i], i, this);
        }

        for (int i = 0; i < Data.Defn.CraftingSpots.Count; i++)
        {
            var spot = Instantiate(scene.CraftingSpotPrefab);
            spot.transform.SetParent(transform, false);
            spot.Initialize(Data.CraftingSpots[i], i, this, scene);
        }
    }

    void OnDestroy()
    {
        if (Data != null)
            Data.OnLocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged()
    {
        if (dragState == DragState.Dragging)
            transform.position = new(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, -6);
        else
            transform.position = (Vector3)Data.Location.WorldLoc;
    }

    void Update()
    {
        StorageFullIndicator.SetActive(Data.Defn.CanStoreItems && Data.IsStorageFull);
        PausedIndicator.SetActive(Data.IsPaused);

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
                var snap = .5f;
                mousePosition.x = Mathf.Round(mousePosition.x * snap) / snap;
                mousePosition.y = Mathf.Round(mousePosition.y * snap) / snap;
            }
            scene.Map.Town.MoveBuilding(Data, new(mousePosition.x, mousePosition.y));
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragState = DragState.PreDrag;
            dragStartPoint = GetMouseWorldPosition();
            offset = transform.position - dragStartPoint;
        }
    }

    void OnMouseUp()
    {
        if (dragState == DragState.Dragging)
        {
            dragState = DragState.NotDragging;
            Data.Location.WorldLoc = new(transform.position.x, transform.position.y);

            putBuildingOnTopOfOthers();
        }
        else
        {
            dragState = DragState.NotDragging;
            if (!EventSystem.current.IsPointerOverGameObject())
                scene.OnBuildingClicked(this);
        }
    }

    private void putBuildingOnTopOfOthers()
    {
        var buildings = scene.Map.GetBuildingGOs();
        foreach (var building in buildings)
            if (building != this)
                building.transform.position = new(building.transform.position.x, building.transform.position.y, building == this ? -4 : -2);
    }


    private void putBuildingOnTopOfOthers2()
    {
        var buildings = scene.Map.GetBuildingGOs();
        for (int i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            building.transform.position = new(building.transform.position.x, building.transform.position.y, (building == this ? -4 : -2) - i / 100f);
        }
    }
    Vector3 GetMouseWorldPosition()
    {
        // Convert the mouse screen position to a world position on the same z-axis as the node
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
    }
}