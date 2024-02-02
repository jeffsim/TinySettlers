using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StoragePile : MonoBehaviour
{
    public GameObject ReservedIndicator;
    [NonSerialized] public StoragePileData Data;
    public TextMeshPro Count;

    public SceneWithMap scene;

    internal void Initialize(StoragePileData pile, int index, Building building, SceneWithMap scene)
    {
        Data = pile;
        this.scene = scene;
        name = "Storage " + index;
        transform.position = new Vector3(Data.Location.WorldLoc.x, Data.Location.WorldLoc.y, -5);
        // spot.OnItemRemoved += OnItemRemoved;
    }

    void OnDestroy()
    {
        //  spot.OnItemRemoved -= OnItemRemoved;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnStoragePileClicked(this);
    }

    private void OnItemRemoved(ItemData item)
    {

    }

    void Update()
    {
        Count.text = Data.NumItemsInPile.ToString();
        // Set color to first item, assumes all are same itemtype.  
        // TODO: render as 'pile'; e.g. render 3x3 smaller items.
        var spot = Data.StorageSpots[0];
        if (spot.HasItem)
        {
            GetComponentInChildren<Renderer>().material.color = spot.ItemContainer.Item.Defn.Color;
            name = "Storage " + Data.IndexInStorageArea + " - " + spot.ItemContainer.Item.Defn.FriendlyName;
        }
        else
            GetComponentInChildren<Renderer>().material.color = Color.black;
        ReservedIndicator.SetActive(spot.Reservation.IsReserved);
    }
}
