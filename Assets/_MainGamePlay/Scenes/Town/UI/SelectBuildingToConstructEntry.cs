using TMPro;
using UnityEngine;

public class SelectBuildingToConstructEntry : MonoBehaviour
{
    SelectBuildingToConstructDialog dialog;
    BuildingDefn buildingDefn;
    public TextMeshProUGUI Name;

    internal void InitializeForBuilding(SelectBuildingToConstructDialog dialog, BuildingDefn buildingDefn)
    {
        this.dialog = dialog;
        this.buildingDefn = buildingDefn;
        Name.text = buildingDefn.FriendlyName;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}