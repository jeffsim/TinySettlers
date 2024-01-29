using UnityEngine;

public class SelectBuildingToConstructDialog : MonoBehaviour
{
    SceneWithMap scene;
    public GameObject List;
    public SelectBuildingToConstructEntry SelectBuildingToConstructEntryPrefab;

    internal void ShowForTile(SceneWithMap scene, Tile tile)
    {
        gameObject.SetActive(true);
        List.RemoveAllChildren();
        
        // position this object over the center of the tile.  Keep in mind that the tile's WorldLoc is in 3D while this object is in the Canvas
        transform.position = Camera.main.WorldToScreenPoint(new Vector3(tile.Data.WorldX, tile.Data.WorldY, 0));

        foreach (var defn in GameDefns.Instance.BuildingDefns.Values)
        {
            if (!defn.CanBeConstructed || defn.IsTestBuilding) continue;
            var entry = Instantiate(SelectBuildingToConstructEntryPrefab, List.transform);
            entry.InitializeForBuilding(scene, tile, this, defn);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}