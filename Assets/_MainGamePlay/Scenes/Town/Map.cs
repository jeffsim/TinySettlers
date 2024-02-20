using System;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [NonSerialized] public TownData Town;
    SceneWithMap scene;
    GameObject ItemsFolder;
    GameObject BuildingsFolder;
    GameObject TilesFolder;
    GameObject WorkersFolder;
    public TimeOfDayMgr TimeOfDayMgr;

    public void Initialize(SceneWithMap scene, TownData townData)
    {
        this.scene = scene;
        Town = townData;
        gameObject.RemoveAllChildren();

        // Set up folders for hierarchy cleanliness
        TilesFolder = addFolder("Tiles");
        BuildingsFolder = addFolder("Buildings");
        ItemsFolder = addFolder("Items");
        WorkersFolder = addFolder("Workers");
        // BuildingsFolder.transform.position = new(0, -1.93f, 0);

        foreach (var tile in Town.Tiles)
            addTileGO(tile);

        foreach (var worker in Town.TownWorkerMgr.Workers)
            addWorkerGO(worker);

        foreach (var building in Town.AllBuildings)
            addBuildingGO(building);

        foreach (var item in Town.ItemsOnGround)
            addItemOnGroundGO(item);

        Town.TownWorkerMgr.OnWorkerCreated += addWorkerGO;
        Town.OnBuildingAdded += OnBuildingAdded;
        Town.OnBuildingRemoved += OnBuildingRemoved;
        Town.OnBuildingMoved += OnBuildingMoved;
        Town.OnItemAddedToGround += addItemOnGroundGO;
        Town.OnItemRemovedFromGround += OnItemRemovedFromGround;
    }

    private void OnBuildingRemoved(BuildingData data)
    {
        scene.NeedsPathUpdate = true;
    }

    private void OnBuildingMoved(BuildingData data)
    {
        scene.NeedsPathUpdate = true;
    }

    private void OnBuildingAdded(BuildingData data)
    {
        addBuildingGO(data);
        scene.NeedsPathUpdate = true;
    }

    void OnDestroy()
    {
        if (Town == null) return;
        Town.TownWorkerMgr.OnWorkerCreated -= addWorkerGO;
        Town.OnBuildingAdded -= OnBuildingAdded;
        Town.OnBuildingRemoved -= OnBuildingRemoved;
        Town.OnBuildingMoved -= OnBuildingMoved;
        Town.OnItemAddedToGround -= addItemOnGroundGO;
        Town.OnItemRemovedFromGround -= OnItemRemovedFromGround;
        
        // Town.OnItemSold -= OnItemSold;
    }

    private void addTileGO(TileData tile)
    {
        if (this == null) return; // destroyed
        var tileGO = Instantiate(scene.TilePrefab);
        tileGO.transform.SetParent(TilesFolder.transform, false);
        tileGO.Initialize(tile, scene);
    }

    private void addWorkerGO(WorkerData worker)
    {
        if (this == null) return; // destroyed
        var workerGO = Instantiate(scene.WorkerPrefab);
        workerGO.transform.SetParent(WorkersFolder.transform, false);
        workerGO.Initialize(worker, scene);
    }

    private void addBuildingGO(BuildingData building)
    {
        if (this == null) return; // destroyed
        var buildingGO = Instantiate(scene.BuildingPrefab);
        buildingGO.transform.SetParent(BuildingsFolder.transform, false);
        buildingGO.Initialize(building, scene);
    }
    public List<Building> GetBuildingGOs()
    {
        var buildings = new List<Building>();
        foreach (Transform child in BuildingsFolder.transform)
        {
            var building = child.GetComponent<Building>();
            if (building != null)
                buildings.Add(building);
        }
        return buildings;
    }

    private void addItemOnGroundGO(ItemData item)
    {
        if (this == null) return; // destroyed
        var itemGO = Instantiate(scene.ItemPrefab);
        itemGO.transform.SetParent(ItemsFolder.transform, false);
        itemGO.Initialize(item, scene);
    }

    private void OnItemRemovedFromGround(ItemData item)
    {
        if (this == null) return; // destroyed
        var itemGO = getItemGO(item);
        if (itemGO != null)
            Destroy(itemGO.gameObject);
    }

    private Item getItemGO(ItemData item)
    {
        foreach (Transform child in ItemsFolder.transform)
        {
            var itemGO = child.GetComponent<Item>();
            if (itemGO != null && itemGO.Data == item)
                return itemGO;
        }
        return null;
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
        Town.TownWorkerMgr.DestroyWorker(worker.Data);
        Destroy(worker.gameObject);
    }

    public void Update()
    {
        if (scene.NeedsPathUpdate)
            scene.UpdatePaths();

        if (Input.GetKeyUp(KeyCode.Space))
            GameTime.TogglePause();
    }

    internal Tile getTileAt(Vector3 position)
    {
        Ray ray = Camera.main.ScreenPointToRay(position);
        if (Physics.Raycast(ray, out RaycastHit hit, 500, LayerMask.GetMask("Tile")))
            return hit.collider.transform.parent.GetComponentInParent<Tile>();

        return null;
    }

    internal bool IsValidDropSpotForBuilding(Vector3 mousePosition, Building building)
    {
        if (Settings.Current.AllowFreeBuildingPlacement)
            return true;

        var tile = getTileAt(Input.mousePosition);
        if (tile == null) return false;
        if (!tile.Data.Defn.PlayerCanBuildOn) return false;
        if (tile.Data.BuildingInTile != null && tile.Data.BuildingInTile != building.Data) return false;
        return true;
    }
}