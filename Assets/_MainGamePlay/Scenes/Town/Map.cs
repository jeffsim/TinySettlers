using System;
using UnityEngine;

public class Map : MonoBehaviour
{
    [NonSerialized] public TownData Town;
    SceneWithMap scene;
    GameObject ItemsFolder;

    public void OnKeyDown()
    {
        Debug.Log("Asdf");
    }
    public void Initialize(SceneWithMap scene, TownData townData)
    {
        this.scene = scene;
        Town = townData;
        gameObject.RemoveAllChildren();

        // Set up folders for hierarchy cleanliness
        var TilesFolder = addFolder("Tiles");
        var BuildingsFolder = addFolder("Buildings");
        ItemsFolder = addFolder("Items");
        var WorkersFolder = addFolder("Workers");

        foreach (var tile in townData.Tiles)
        {
            var tileGO = Worker.Instantiate<Tile>(scene.TilePrefab);
            tileGO.transform.SetParent(TilesFolder.transform, false);
            tileGO.Initialize(tile, scene);
        }

        foreach (var worker in townData.Workers)
        {
            var workerGO = Worker.Instantiate<Worker>(scene.WorkerPrefab);
            workerGO.transform.SetParent(WorkersFolder.transform, false);
            workerGO.Initialize(worker, scene);
        }

        foreach (var building in townData.Buildings)
        {
            var buildingGO = Worker.Instantiate<Building>(scene.BuildingPrefab);
            buildingGO.transform.SetParent(BuildingsFolder.transform, false);
            buildingGO.Initialize(building, scene);
        }

        // foreach (var tile in townData.InitialItemsOnGround)
        // {
        // }

        townData.OnItemAddedToGround += ItemAddedToGround;
    }

    private void ItemAddedToGround(ItemData item)
    {
        if (this == null) return; // destroyed
        var itemGO = Instantiate<Item>(scene.ItemOnGroundPrefab);
        itemGO.transform.SetParent(ItemsFolder.transform, false);
        itemGO.Initialize(item, scene);
    }

    private GameObject addFolder(string name)
    {
        var folder = new GameObject(name);
        folder.transform.parent = transform;
        return folder;
    }

    internal void DestroyBuilding(Building building)
    {
        Town.DestroyBuilding(building.Data);
        Destroy(building.gameObject);
    }

    internal void DestroyWorker(Worker worker)
    {
        Town.DestroyWorker(worker.Data);
        Destroy(worker.gameObject);
    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            GameTime.TogglePause();

        int numReservedStorageSpots = 0;
        foreach (var building in Town.Buildings)
            foreach (var area in building.StorageAreas)
                foreach (var spot in area.StorageSpots)
                    if (spot.IsReserved) numReservedStorageSpots++;

        scene.DebugInfo.text = "Reserved spots: " + numReservedStorageSpots;
    }

    internal Tile getTileAt(Vector3 position)
    {
        Ray ray = Camera.main.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit, 500, LayerMask.GetMask("Tile")))
            return hit.collider.transform.parent.GetComponentInParent<Tile>();

        return null;
    }
}