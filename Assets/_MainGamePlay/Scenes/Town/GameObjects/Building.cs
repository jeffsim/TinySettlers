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

        Data.OnBuildingTileLocChanged += TileLocChanged;

        Name.text = data.Defn.FriendlyName;
        GetComponentInChildren<Renderer>().material.color = data.Defn.BuildingColor;
        transform.position = new Vector3(data.WorldLoc.x, data.WorldLoc.y, BuildingZ);

        for (int i = 0; i < Data.Defn.NumStorageAreas; i++)
        {
            var item = StorageArea.Instantiate(scene.BuildingStorageAreaPrefab);
            item.transform.SetParent(StorageEditorFolder.transform, false);
            item.Initialize(Data.StorageAreas[i], this, scene.BuildingStorageSpotPrefab, StorageEditorFolder.transform);
        }

        for (int i = 0; i < Data.Defn.GatheringSpots.Count; i++)
        {
            var spot = GatheringSpot.Instantiate(scene.GatheringSpotPrefab);
            spot.transform.SetParent(transform, false);
            spot.Initialize(scene, Data.GatheringSpots[i], i, this);
        }

        for (int i = 0; i < Data.Defn.CraftingSpots.Count; i++)
        {
            var spot = CraftingSpot.Instantiate(scene.CraftingSpotPrefab);
            spot.transform.SetParent(transform, false);
            spot.Initialize(Data.CraftingSpots[i], i, this, scene);
        }
    }

    void OnDestroy()
    {
        if (Data != null)
            Data.OnBuildingTileLocChanged -= TileLocChanged;
    }

    private void TileLocChanged()
    {
        transform.position = new Vector3(Data.WorldLoc.x, Data.WorldLoc.y, BuildingZ);
    }

    public void OnMouseDown()
    {
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

            var tile = scene.Map.getTileAt(Input.mousePosition);
            Debug.Assert(tile != null, "null tile at " + Input.mousePosition);
            scene.Map.Town.MoveBuilding(Data, tile.Data.TileX, tile.Data.TileY);
        }
    }

    void Update()
    {
        StorageFullIndicator.SetActive(Data.Defn.CanStoreItems && Data.IsStorageFull);
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