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
        transform.position = Camera.main.WorldToScreenPoint(new Vector3(tile.Data.WorldX, 0, tile.Data.WorldY));

        foreach (var defn in GameDefns.Instance.BuildingDefns.Values)
        {
            if (!defn.CanBeConstructed || defn.IsTestBuilding) continue;
            var entry = Instantiate(SelectBuildingToConstructEntryPrefab, List.transform);
            entry.InitializeForBuilding(scene, tile, this, defn);
        }
    }

    // Only used when player clicks on background; used in AllowFreeBuildingPlacement mode
    internal void ShowAtWorldLoc(SceneWithMap scene, Vector3 screenPosition)
    {
        if (Settings.Current.AllowFreeBuildingPlacement)
        {
            gameObject.SetActive(true);
            List.RemoveAllChildren();
            List.RemoveAllChildren();

            // position this object over the center of the tile.  Keep in mind that the tile's WorldLoc is in 3D while this object is in the Canvas
            transform.position = Camera.main.WorldToScreenPoint(new Vector3(screenPosition.x, 0, screenPosition.z));

            var worldLoc = Camera.main.ScreenToWorldPoint(transform.position);
            worldLoc.y = Settings.Current.BuildingsY;
            
            foreach (var defn in GameDefns.Instance.BuildingDefns.Values)
            {
                if (!defn.CanBeConstructed || defn.IsTestBuilding) continue;
                var entry = Instantiate(SelectBuildingToConstructEntryPrefab, List.transform);

                entry.InitializeForBuilding(scene, worldLoc, this, defn);
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}