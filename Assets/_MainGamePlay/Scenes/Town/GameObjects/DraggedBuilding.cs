using System;
using UnityEngine;

public class DraggedBuilding : MonoBehaviour
{
    [NonSerialized] Building building;

    public GameObject validDropSpot;
    public GameObject invalidDropSpot;

    public void Initialize(BuildingDefn defn, Building building)
    {
        this.building = building;
        GetComponentInChildren<Renderer>().material.color = defn.BuildingColor;
    }

    public void updatePosition(Vector3 loc)
    {
        transform.rotation = Settings.Current.UseOrthographicCamera ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
        transform.localScale = Settings.Current.UseOrthographicCamera ? Vector3.one : new Vector3(1, 0.01f, 1);
        transform.position = loc;
        var validDropSpotForBuilding = building.scene.Map.IsValidDropSpotForBuilding(Input.mousePosition, building);
        validDropSpot.SetActive(validDropSpotForBuilding);
        invalidDropSpot.SetActive(!validDropSpotForBuilding);
    }
}