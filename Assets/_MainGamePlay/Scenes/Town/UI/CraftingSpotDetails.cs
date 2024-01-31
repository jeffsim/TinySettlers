using System;
using TMPro;
using UnityEngine;

public class CraftingSpotDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Item;
    public TextMeshProUGUI Reservation;
    public CraftingSpot spot;
    SceneWithMap scene;

    public void ShowForCraftingSpot(SceneWithMap scene, CraftingSpot spot)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.spot = spot;
        Name.text = "Crafting spot (" + spot.Data.InstanceId + ")";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    void Update()
    {
        if (spot == null) return;

        if (spot.Data.Reservation.IsReserved)
        {
            Reservation.text = "Reservation:";
            Reservation.text += "\n    By: " + spot.Data.Reservation.ReservedBy;
            Reservation.text += "\n    Task: " + spot.Data.Reservation.ReservedBy.CurrentTask;
            Reservation.text += "\n    Item: " + spot.Data.Reservation.ReservedBy.CurrentTask.GetTaskItem().FriendlyName;
        }
        else
            Reservation.text = "Not reserved";

        var str = "Crafting resources:";
        foreach (var resource in spot.Data.Building.Defn.CraftableItems)
            str += "\n    " + resource.Id;
        Item.text = str;
    }
}
