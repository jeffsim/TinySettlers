using TMPro;
using UnityEditor.Compilation;
using UnityEngine;

public class SceneWithMap : SceneMgr
{
    public Map Map;
    public bool NeedsPathUpdate;

    public AstarPath Pathfinder;
    public Tile TilePrefab;
    public Building BuildingPrefab;
    public DraggedBuilding DraggedBuildingPrefab;
    public Worker WorkerPrefab;

    public StorageArea BuildingStorageAreaPrefab;
    public StoragePile BuildingStoragePilePrefab;
    public GatheringSpot GatheringSpotPrefab;
    public CraftingSpot CraftingSpotPrefab;
    public SleepingSpot SleepingSpotPrefab;
    public Item ItemPrefab;

    // UI
    public WorkerDetails WorkerDetails;
    public BuildingDetails BuildingDetails;
    public StorageSpotDetails StorageSpotDetails;
    public GatheringSpotDetails GatheringSpotDetails;
    public CraftingSpotDetails CraftingSpotDetails;
    public ItemOnGroundDetails ItemOnGroundDetails;
    public AllNeedsDetails AllNeedsDetails;
    public SelectBuildingToConstructDialog SelectBuildingToConstruct;
    public AvailableTasksDialog AvailableTasksDialog;

    public Background Background;

    public GameObject RecompilingText;
    public TextMeshProUGUI DebugInfo;

    public bool Debug_DrawPaths = true;

#if UNITY_EDITOR
    bool reloadOnNextOnEnable;
#endif

    public override void OnEnable()
    {
        base.OnEnable();

#if UNITY_EDITOR
        CompilationPipeline.compilationStarted += (c) =>
        {
            if (Application.isPlaying)
            {
                if (RecompilingText != null)
                    RecompilingText.gameObject.SetActive(true);
                gameDataMgr.SaveProfile();
            }
        };

        CompilationPipeline.compilationFinished += (c) =>
        {
            if (Application.isPlaying)
            {
                if (RecompilingText != null)
                    RecompilingText.gameObject.SetActive(false);
                reloadOnNextOnEnable = true;
            }
        };

        // Recompile support
        if (reloadOnNextOnEnable)
        {
            reloadOnNextOnEnable = false;
            CreateMap(gameDataMgr.GameData.CurrentTown);
        }
#endif
        Background.Initialize(this);
        HideAllDialogs();

        // AllNeedsDetails.Show(this, SortNeedsDisplayBy.Priority);
    }

    internal void DestroyBuilding(Building building)
    {
        Map.DestroyBuilding(building);
        BuildingDetails.Hide();
    }

    internal void DestroyWorker(Worker worker)
    {
        Map.DestroyWorker(worker);
        WorkerDetails.Hide();
    }

    public void OnWorkerClicked(Worker worker)
    {
        HideAllDialogs();
        WorkerDetails.ShowForWorker(this, worker);
    }

    public void OnBuildingClicked(Building building)
    {
        HideAllDialogs();
        BuildingDetails.ShowForBuilding(this, building);
    }

    // public void OnStorageSpotClicked(StorageSpot storageSpot)
    // {
    //     HideAllDialogs();
    //     StorageSpotDetails.ShowForStoragePile(this, storageSpot);
    // }

    public void OnStoragePileClicked(StoragePile pile)
    {
        HideAllDialogs();
        StorageSpotDetails.ShowForStoragePile(this, pile);
    }

    public void OnGatheringSpotClicked(GatheringSpot spot)
    {
        HideAllDialogs();
        GatheringSpotDetails.ShowForGatheringSpot(this, spot);
    }

    public void OnCraftingSpotClicked(CraftingSpot spot)
    {
        HideAllDialogs();
        CraftingSpotDetails.ShowForCraftingSpot(this, spot);
    }

    public void OnItemOnGroundClicked(Item item)
    {
        HideAllDialogs();
        ItemOnGroundDetails.ShowForItemOnGround(this, item);
    }

    public void OnShowAllNeedsClicked(int sortBy)
    {
        HideAllDialogs();
        AllNeedsDetails.Show(this, (SortNeedsDisplayBy)sortBy);
    }

    public void OnAvailableTasksClicked()
    {
        HideAllDialogs();
        AvailableTasksDialog.Show(this);
    }

    public bool AnyDialogIsOpen()
    {
        return WorkerDetails.gameObject.activeSelf || BuildingDetails.gameObject.activeSelf ||
               StorageSpotDetails.gameObject.activeSelf || GatheringSpotDetails.gameObject.activeSelf ||
               CraftingSpotDetails.gameObject.activeSelf || ItemOnGroundDetails.gameObject.activeSelf ||
               AllNeedsDetails.gameObject.activeSelf || SelectBuildingToConstruct.gameObject.activeSelf ||
               AvailableTasksDialog.gameObject.activeSelf;
    }

    public void HideAllDialogs()
    {
        AllNeedsDetails.Hide();
        StorageSpotDetails.Hide();
        GatheringSpotDetails.Hide();
        CraftingSpotDetails.Hide();
        ItemOnGroundDetails.Hide();
        BuildingDetails.Hide();
        WorkerDetails.Hide();
        SelectBuildingToConstruct.Hide();
        AvailableTasksDialog.Hide();
    }

    public void OnAddWorkerClicked()
    {
        Map.Town.CreateWorkerInBuilding(Map.Town.Camp);
    }

    public void Debug_OnEmptyBuildingStorage(Building building)
    {
        building.Data.Debug_RemoveAllItemsFromStorage();
    }

    protected virtual void CreateMap(TownData town)
    {
        // Destroy current Map (if any)
        if (Map != null)
            DestroyImmediate(Map.gameObject);

        // Create the gameobject that contains the Map and add and initialize a Map on it
        var mapGO = new GameObject("Map");
        Map = mapGO.AddComponent<Map>();
        Map.Initialize(this, town);

        FindAnyObjectByType<TimeOfDayMgr>(FindObjectsInactive.Exclude).InitializeForTown(town);
        FindAnyObjectByType<TopBar>(FindObjectsInactive.Exclude).InitializeForTown(town);

        NeedsPathUpdate = true;
    }
    
    public void UpdatePaths()
    {
        // Pathfinder.Scan();
        var graphToScan = AstarPath.active.data.gridGraph;
        if (graphToScan != null)
        {
            AstarPath.active.Scan(graphToScan);
            AstarPath.active.FlushGraphUpdates();
            NeedsPathUpdate = false;
        }
    }

    internal void OnTileClicked(Tile tile)
    {
        if (AnyDialogIsOpen())
            HideAllDialogs();
        else
        {
            // Debug.Log("tile clicked " + tile.Data.TileX + " " + tile.Data.TileY);
            if (tile.Data.BuildingInTile == null)
            {
                // User clicked empty tile; show the 'select building to construct' dialog
                SelectBuildingToConstruct.ShowForTile(this, tile);
            }
        }
    }

    internal void PlayerSelectedBuildingToConstructInTile(BuildingDefn buildingDefn, TileData tile)
    {
        var building = Map.Town.ConstructBuilding(buildingDefn, tile.TileX, tile.TileY);

        // Autoassign one worker to the newly constructed building
        if (buildingDefn.HasWorkers)
            Map.Town.AssignWorkerToBuilding(building);
        SelectBuildingToConstruct.Hide();
    }

    internal void PlayerSelectedBuildingToConstructAtWorldLoc(BuildingDefn buildingDefn, Vector3 worldLoc)
    {
        var building = Map.Town.ConstructBuilding(buildingDefn, worldLoc);

        // Autoassign one worker to the newly constructed building
        if (buildingDefn.HasWorkers)
            Map.Town.AssignWorkerToBuilding(building);
        SelectBuildingToConstruct.Hide();
    }
}