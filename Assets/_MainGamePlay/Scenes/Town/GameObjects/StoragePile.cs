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

    internal void Initialize(StorageAreaData areaData, StoragePileData pileData, int index, Building building, SceneWithMap scene)
    {
        Data = pileData;
        this.scene = scene;
        name = "Storage " + index;
        transform.position = pileData.Location.WorldLoc;
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
        if (Data.StorageSpots.Count > 1)
            Count.text = Data.NumItemsInPile.ToString();
        else
            Count.text = Data.StorageSpots[0].Container.HasItem ? Data.StorageSpots[0].Container.FirstItem.DefnId[0..2] : "";
        // Set color to first item, assumes all are same itemtype.  
        // TODO: render as 'pile'; e.g. render 3x3 smaller items.
        bool isAnySpotReserved = Data.StorageSpots.Any(s => s.Reservable.IsReserved);
        if (Data.NumItemsInPile > 0)
        {
            var firstSpotWithItem = Data.StorageSpots.FirstOrDefault(s => s.Container.HasItem);
            Debug.Assert(firstSpotWithItem != null);
            GetComponentInChildren<Renderer>().material.color = firstSpotWithItem.Container.FirstItem.Defn.Color;
            name = "Storage " + Data.IndexInStorageArea + " - " + firstSpotWithItem.Container.FirstItem.Defn.FriendlyName;
        }
        else
            GetComponentInChildren<Renderer>().material.color = Color.black;
        ReservedIndicator.SetActive(isAnySpotReserved);


        // Draw line to all workers that have reserved this cell if it's currently showing details dialog
        foreach (var spot in Data.StorageSpots)
            if (scene.StorageSpotDetails.gameObject.activeSelf && scene.StorageSpotDetails.pile.Data.StorageSpots.Contains(spot))
            {
                if (spot.Reservable.IsReserved)
                {
                    using (Drawing.Draw.ingame.WithColor(Color.red))
                    using (Drawing.Draw.ingame.WithLineWidth(2))
                    {
                        Vector3 loc1 = new(transform.position.x, transform.position.y, -6);
                        Vector3 loc2 = new(spot.Reservable.ReservedBy.Location.WorldLoc.x, -6, spot.Reservable.ReservedBy.Location.WorldLoc.y);
                        Drawing.Draw.ingame.Line(loc1, loc2);
                    }
                }
            }
    }
}
