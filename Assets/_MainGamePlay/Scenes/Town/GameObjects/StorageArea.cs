using UnityEngine;

public class StorageArea : MonoBehaviour
{
    public GameObject Background;

    internal void Initialize(StorageAreaData areaData, StorageAreaDefn areaDefn, Building building, StoragePile prefab, Transform editorFolder)
    {
        name = "Storage Area";
        transform.localPosition = areaData.Location.GetWorldLocRelativeTo(building.Data.Location, -.25f);
        Background.transform.localScale = new Vector3(1.1f * areaDefn.StorageAreaSize.x + .1f, 1.1f * areaDefn.StorageAreaSize.y + .1f, .1f);
        for (int i = 0; i < areaData.StoragePiles.Count; i++)
        {
            var item = Instantiate(prefab);
            item.transform.SetParent(transform, false);
            item.Initialize(areaData, areaData.StoragePiles[i], i, building, building.scene);
        }
    }

    void Update()
    {
    }
}
