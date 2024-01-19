using System;
using UnityEngine;

public class StorageArea : MonoBehaviour
{
    [NonSerialized] StorageAreaData areaData;
    public GameObject Background;
    int index;

    internal void Initialize(StorageAreaData areaData, Building building, StorageSpot prefab, Transform editorFolder)
    {
        this.areaData = areaData;
        name = "Storage Area";
        transform.position = new Vector3(areaData.WorldLoc.x, areaData.WorldLoc.y, -3);
        // hack
        if (areaData.Building.Defn.StorageAreaWidthAndHeight == 2)
        {
            Background.transform.localScale = new Vector3(2.4f, 2.4f, 3);
            Background.transform.localPosition = new Vector3(-.55f, .55f, 0);
        }
        else // 3
        {
            Background.transform.localScale = new Vector3(3.5f, 3.5f, 3);
            Background.transform.localPosition = new Vector3(0, 0, 0);
        }

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
