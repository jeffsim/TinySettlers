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
        var validDropSpotForBuilding = building.scene.Map.IsValidDropSpotForBuilding(Input.mousePosition, building);
        validDropSpot.SetActive(validDropSpotForBuilding);
        invalidDropSpot.SetActive(!validDropSpotForBuilding);
    }
}
