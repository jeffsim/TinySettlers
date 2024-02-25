using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Map : MonoBehaviour
{
    [NonSerialized] public TownData Town;
    SceneWithMap scene;
    GameObject ItemsFolder;
    GameObject BuildingsFolder;
    GameObject TilesFolder;
    GameObject WorkersFolder;
    GameObject DebugFolder;
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

        if (Settings.Current.ShowHexDebug)
        {
            var curDebugFolder = transform.Find("Debug");
            if (curDebugFolder != null)
                Destroy(curDebugFolder);
            DebugFolder = addFolder("Debug");

            // Debug spheres to see where hexes are
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                {
                    var worldPos = Utilities.ConvertHexTileToWorldPos(new Vector2Int(x, y));
                    addSphere(new Vector3(worldPos.x, 2, worldPos.z), 1.5f, Color.red);
                    addLabel(new Vector3(worldPos.x, 2, worldPos.z - 2), "(" + x + ", " + y + ")", 6, Color.yellow);
                }
            mouseSphere = addSphere(Vector3.zero, 1f, Color.blue);
            hexSphere = addSphere(Vector3.zero, 1.75f, Color.green);
        }
    }

    private void addLabel(Vector3 pox, string text, int fontSize, Color color)
    {
        var label = new GameObject("Label");
        label.transform.parent = DebugFolder.transform;
        label.transform.position = pox;
        var textMesh = label.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.transform.Rotate(45, 0, 0);
        textMesh.color = color;
    }

    GameObject mouseSphere, hexSphere;

    private void testDrawPath()
    {
        Vector3 worldPos = Utilities.GetMouseWorldPosition();
        worldPos.y = 2;
        mouseSphere.transform.position = worldPos;

        Test(worldPos);

        var hexTile = Utilities.GetCenterOfHexTileClosestToWorldPos(worldPos);
        Debug.Log("B: " + hexTile+ ", " + worldPos);
        hexTile.y = 2;
        hexSphere.transform.position = hexTile;

        using (Drawing.Draw.ingame.WithColor(Color.green))
        using (Drawing.Draw.ingame.WithLineWidth(2))
            Drawing.Draw.ingame.Line(worldPos, hexTile);
    }

    private void Test(Vector3 worldPos)
    {
        var hexTile = Utilities.ConvertWorldPosToHexTile(worldPos);
        var hexTileCenterWorldPos = Utilities.ConvertHexTileToWorldPos(hexTile);
        // Debug.Log("C: " + hexTileCenterWorldPos);
        var p0to1 = (worldPos - hexTileCenterWorldPos) / TileData.TileSize + Vector3.one / 2f;
        var isInFrontQuarter = p0to1.x < .25f;
        var isInTopHalf = p0to1.z > .5f;

        p0to1.x *= 5;
        p0to1.z = (p0to1.z - .5f) * 2f;
        // Debug.Log("x: " + p0to1.x + ", z:" + p0to1.z);
        if (isInFrontQuarter)
        {
            if (isInTopHalf)
            {
                if (p0to1.x > p0to1.z)
                    mouseSphere.GetComponent<Renderer>().material.color = Color.cyan; // this one
                else
                {
                    if ((hexTile.x & 1) == 1)
                        mouseSphere.GetComponent<Renderer>().material.color = Color.black; // x-1,y+1
                    else
                        mouseSphere.GetComponent<Renderer>().material.color = Color.red; // x-1
                }
            }
            else
            {
                if (p0to1.x > -p0to1.z)
                    mouseSphere.GetComponent<Renderer>().material.color = Color.yellow; // this one
                else
                {
                    if ((hexTile.x & 1) == 1)
                        mouseSphere.GetComponent<Renderer>().material.color = Color.magenta; // x-1
                    else
                        mouseSphere.GetComponent<Renderer>().material.color = Color.green; // x-1, y-1
                }
            }
        }
        else
            mouseSphere.GetComponent<Renderer>().material.color = Color.blue;
    }

    private GameObject addSphere(Vector3 pos, float scale, Color color)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = DebugFolder.transform;
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * scale;
        sphere.GetComponent<Renderer>().material.color = color;
        return sphere;
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
        if (Settings.Current.ShowHexDebug)
            testDrawPath();

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