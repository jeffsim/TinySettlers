using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class StorageSpot : MonoBehaviour
{
    public GameObject ReservedIndicator;
    [NonSerialized] public StorageSpotData Data;

    int index;
    public SceneWithMap scene;

    internal void Initialize(StorageSpotData spot, int index, Building building, SceneWithMap scene)
    {
        this.Data = spot;
        this.index = index;
        this.scene = scene;
        name = "Storage " + index;
        transform.position = new Vector3(spot.WorldLoc.x, spot.WorldLoc.y, -5);

        // spot.OnItemRemoved += OnItemRemoved;
    }

    void OnDestroy()
    {
        //  spot.OnItemRemoved -= OnItemRemoved;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnStorageSpotClicked(this);
    }

    private void OnItemRemoved(ItemData item)
    {

    }

    void Update()
    {
        if (Data.IsEmpty)
            GetComponentInChildren<Renderer>().material.color = Color.black;
        else
        {
            GetComponentInChildren<Renderer>().material.color = Data.ItemInStorage.Defn.Color;
            name = "Storage " + index + " - " + Data.ItemInStorage.Defn.FriendlyName;
        }
        ReservedIndicator.SetActive(Data.IsReserved);
    }
}
