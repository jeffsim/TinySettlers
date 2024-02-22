using System;
using TMPro;
using UnityEngine;

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

    public BuildingBase BuildingBase;

    public void Initialize(BuildingData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        BuildingBase.InitializeForBuilding(this, scene, data);

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
        transform.position = Data.Location.WorldLoc;
    }

    void Update()
    {
        StorageFullIndicator.SetActive(Data.Defn.CanStoreItems && Data.IsStorageFull);
        PausedIndicator.SetActive(Data.IsPaused);
    }
}