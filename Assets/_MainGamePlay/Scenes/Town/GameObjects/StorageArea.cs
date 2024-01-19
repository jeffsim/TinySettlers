using System;
using UnityEngine;

public class StorageArea : MonoBehaviour
{
    [NonSerialized] StorageAreaData areaData;

    int index;
    
    internal void Initialize(StorageAreaData areaData, Building building, StorageSpot prefab, Transform editorFolder)
    {
        this.areaData = areaData;
        name = "Storage Area";
        transform.position = new Vector3(areaData.WorldLoc.x, areaData.WorldLoc.y, -3);

        for (int i = 0; i < areaData.StorageSpots.Count; i++)
        {
            var item = StorageSpot.Instantiate(prefab);
            item.transform.SetParent(transform, false);
            item.Initialize(areaData.StorageSpots[i], i, building, building.scene);
        }
    }

    void Update()
    {
        // if (spot.IsEmpty)
        //     GetComponentInChildren<Renderer>().material.color = Color.black;
        // else
        // {
        //     GetComponentInChildren<Renderer>().material.color = spot.ItemInStorage.Defn.Color;
        //     name = "Storage " + index + " - " + spot.ItemInStorage.Defn.FriendlyName;
        // }
        // ReservedIndicator.SetActive(spot.IsReserved);
    }
}
