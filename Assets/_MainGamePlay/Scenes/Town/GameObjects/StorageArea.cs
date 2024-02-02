using UnityEngine;

public class StorageArea : MonoBehaviour
{
    public GameObject Background;

    internal void Initialize(StorageAreaData areaData, StorageAreaDefn areaDefn, Building building, StoragePile prefab, Transform editorFolder)
    {
        name = "Storage Area";
        transform.position = new Vector3(areaData.Location.WorldLoc.x, areaData.Location.WorldLoc.y, -3);
        Background.transform.localScale = new Vector3(1.1f * areaDefn.StorageAreaSize.x + .1f, 1.1f * areaDefn.StorageAreaSize.y + .1f, 3);
        for (int i = 0; i < areaData.StoragePiles.Count; i++)
        {
            var item = Instantiate(prefab);
            item.transform.SetParent(transform, false);
            item.Initialize(areaData.StoragePiles[i], i, building, building.scene);
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
