using System;
using UnityEngine;

public class Map : MonoBehaviour
{
    [NonSerialized] public TownData Town;
    SceneWithMap scene;
    GameObject ItemsFolder;
    GameObject BuildingsFolder;
    GameObject TilesFolder;
    GameObject WorkersFolder;

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
        TilesFolder = addFolder("Tiles");
        BuildingsFolder = addFolder("Buildings");
        ItemsFolder = addFolder("Items");
        WorkersFolder = addFolder("Workers");

        foreach (var tile in Town.Tiles)
            addTileGO(tile);

        foreach (var worker in Town.Workers)
            addWorkerGO(worker);

        foreach (var building in Town.Buildings)
            addBuildingGO(building);

        foreach (var item in Town.ItemsOnGround)
            addItemOnGroundGO(item);

        Town.OnWorkerCreated += addWorkerGO;
        Town.OnBuildingAdded += addBuildingGO;
        Town.OnItemAddedToGround += addItemOnGroundGO;
        Town.OnItemRemovedFromGround += OnItemRemovedFromGround;
        // Town.OnItemSold += OnItemSold;
    }

    void OnDestroy()
    {
        if (Town == null) return;
        Town.OnWorkerCreated -= addWorkerGO;
        Town.OnBuildingAdded -= addBuildingGO;
        Town.OnItemAddedToGround -= addItemOnGroundGO;
        Town.OnItemRemovedFromGround -= OnItemRemovedFromGround;
        // Town.OnItemSold -= OnItemSold;
    }

    private void addTileGO(TileData tile)
    {
        var tileGO = Worker.Instantiate<Tile>(scene.TilePrefab);
        tileGO.transform.SetParent(TilesFolder.transform, false);
        tileGO.Initialize(tile, scene);
    }

    private void addWorkerGO(WorkerData worker)
    {
        var workerGO = Worker.Instantiate<Worker>(scene.WorkerPrefab);
        workerGO.transform.SetParent(WorkersFolder.transform, false);
        workerGO.Initialize(worker, scene);
    }

    private void addBuildingGO(BuildingData building)
    {
        var buildingGO = Worker.Instantiate<Building>(scene.BuildingPrefab);
        buildingGO.transform.SetParent(BuildingsFolder.transform, false);
        buildingGO.Initialize(building, scene);
    }

    private void addItemOnGroundGO(ItemData item)
    {
        if (this == null) return; // destroyed
        var itemGO = Instantiate(scene.ItemOnGroundPrefab);
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
        Town.DestroyWorker(worker.Data);
        Destroy(worker.gameObject);
    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
            GameTime.TogglePause();

        scene.Gold.text = "Gold: " + Town.Gold.ToString();

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

    internal bool IsValidDropSpotForBuilding(Vector3 mousePosition, Building building)
    {
        var tile = getTileAt(Input.mousePosition);
        if (tile == null) return false;
        if (!tile.Data.Defn.PlayerCanBuildOn) return false;
        if (tile.Data.BuildingInTile != null && tile.Data.BuildingInTile != building.Data) return false;
        return true;
    }
}