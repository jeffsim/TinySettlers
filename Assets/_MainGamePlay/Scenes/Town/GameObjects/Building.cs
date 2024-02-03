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

    static float BuildingZ = 0;

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
        GetComponentInChildren<Renderer>().material.color = data.Defn.BuildingColor;
        transform.position = new Vector3(data.Location.WorldLoc.x, data.Location.WorldLoc.y, BuildingZ);

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
        transform.position = new Vector3(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, BuildingZ);
    }

    public void OnMouseDown()
    {
        if (!Data.Defn.PlayerCanMove)
            return;
        dragState = DragState.PreDrag;
        dragStartPoint = Input.mousePosition;
    }

    public void OnMouseDrag()
    {
        if (dragState == DragState.PreDrag)
        {
            if (Vector3.Distance(dragStartPoint, Input.mousePosition) > 10)
            {
                dragState = DragState.Dragging;
                draggingGO = GameObject.Instantiate(scene.DraggedBuildingPrefab);
                draggingGO.Initialize(Data.Defn, this);
                draggingGO.transform.position = transform.position + new Vector3(0, 0, 10);
                dragStartPoint = transform.position;
            }
        }
        else if (dragState == DragState.Dragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, dragStartPoint);
            plane.Raycast(ray, out float distance);
            Vector3 mouseIntersectPoint = ray.GetPoint(distance);

            draggingGO.updatePosition(new Vector3(mouseIntersectPoint.x, mouseIntersectPoint.y, -5));
        }
    }

    public void OnMouseUp()
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

    void Update()
    {
        StorageFullIndicator.SetActive(Data.Defn.CanStoreItems && Data.IsStorageFull);
        PausedIndicator.SetActive(Data.IsPaused);
    }

    // void updateItemsInStorage()
    // {
    //     StorageEditorFolder.RemoveAllChildren();
    //     for (int i = 0; i < Data.ItemsInStorage.Count; i++)
    //     {
    //         var item = BuildingStorageItem.Instantiate(scene.BuildingStorageItemPrefab);
    //         item.transform.SetParent(StorageEditorFolder.transform, false);
    //         item.Initialize(Data.ItemsInStorage[i], i, this);
    //     }
    // }
}