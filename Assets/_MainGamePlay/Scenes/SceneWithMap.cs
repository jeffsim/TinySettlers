using System;
using TMPro;
using UnityEditor.Compilation;
using UnityEngine;

public class SceneWithMap : SceneMgr
{
    public Map Map;

    public Tile TilePrefab;
    public Building BuildingPrefab;
    public DraggedBuilding DraggedBuildingPrefab;
    public Worker WorkerPrefab;

    public StorageArea BuildingStorageAreaPrefab;
    public StorageSpot BuildingStorageSpotPrefab;
    public GatheringSpot GatheringSpotPrefab;
    public CraftingSpot CraftingSpotPrefab;
    public Item ItemOnGroundPrefab;

    // UI
    public WorkerDetails WorkerDetails;
    public BuildingDetails BuildingDetails;
    public StorageSpotDetails StorageSpotDetails;
    public GatheringSpotDetails GatheringSpotDetails;
    public CraftingSpotDetails CraftingSpotDetails;
    public AllNeedsDetails AllNeedsDetails;

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
        hideDialogs();
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
        hideDialogs();
        WorkerDetails.ShowForWorker(this, worker);
    }

    public void OnBuildingClicked(Building building)
    {
        hideDialogs();
        BuildingDetails.ShowForBuilding(this, building);
    }

    public void OnStorageSpotClicked(StorageSpot storageSpot)
    {
        hideDialogs();
        StorageSpotDetails.ShowForStorageSpot(this, storageSpot);
    }

    public void OnGatheringSpotClicked(GatheringSpot spot)
    {
        hideDialogs();
        GatheringSpotDetails.ShowForGatheringSpot(this, spot);
    }

    public void OnCraftingSpotClicked(CraftingSpot spot)
    {
        hideDialogs();
        CraftingSpotDetails.ShowForCraftingSpot(this, spot);
    }

    public void OnShowAllNeedsClicked(int sortBy)
    {
        hideDialogs();
        AllNeedsDetails.Show(this, (SortNeedsDisplayBy)sortBy);
    }

    void hideDialogs()
    {
        AllNeedsDetails.Hide();
        StorageSpotDetails.Hide();
        GatheringSpotDetails.Hide();
        CraftingSpotDetails.Hide();
        BuildingDetails.Hide();
        WorkerDetails.Hide();
    }

    public void OnTestMoveClicked(int test)
    {
        Map.Town.TestMoveBuilding(test);
    }

    public void Debug_OnEmptyBuildingStorage(Building building)
    {
        building.Data.Debug_RemoveAllItemsFromStorage();
    }

    protected virtual void CreateMap(TownData mapData)
    {
        // Destroy current Map (if any)
        if (Map != null)
            DestroyImmediate(Map.gameObject);

        // Create the gameobject that contains the Map and add and initialize a Map on it
        var mapGO = new GameObject("Map");
        Map = mapGO.AddComponent<Map>();
        Map.Initialize(this, mapData);
    }
}