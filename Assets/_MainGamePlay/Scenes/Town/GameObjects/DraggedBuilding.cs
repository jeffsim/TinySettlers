using UnityEngine;

public class DraggedBuilding : MonoBehaviour
{
    Building building;

    public GameObject validDropSpot;
    public GameObject invalidDropSpot;

    public void Initialize(BuildingDefn defn, Building building)
    {
        this.building = building;
        GetComponentInChildren<Renderer>().material.color = defn.BuildingColor;
    }

    public void updatePosition(Vector3 loc)
    {
        transform.position = loc;

        // update if dragged over valid tile
        var tile = building.scene.Map.getTileAt(Input.mousePosition);
        var showValidDropSpot = tile != null && tile.Data.BuildingInTile == null;
        var showInvalidDropSpot = tile == null || (tile.Data.BuildingInTile != null && tile.Data.BuildingInTile != building.Data);
        validDropSpot.SetActive(showValidDropSpot);
        invalidDropSpot.SetActive(showInvalidDropSpot);
    }
}
