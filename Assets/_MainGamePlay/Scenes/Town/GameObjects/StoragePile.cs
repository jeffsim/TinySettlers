using System;
using System.Linq;
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
        bool isAnySpotReserved = Data.StorageSpots.Any(s => s.Reservation.IsReserved);
        if (Data.NumItemsInPile > 0)
        {
            var firstSpotWithItem = Data.StorageSpots.FirstOrDefault(s => s.HasItem);
            Debug.Assert(firstSpotWithItem != null);
            GetComponentInChildren<Renderer>().material.color = firstSpotWithItem.ItemContainer.Item.Defn.Color;
            name = "Storage " + Data.IndexInStorageArea + " - " + firstSpotWithItem.ItemContainer.Item.Defn.FriendlyName;
        }
        else
            GetComponentInChildren<Renderer>().material.color = Color.black;
        ReservedIndicator.SetActive(isAnySpotReserved);
    }
}
