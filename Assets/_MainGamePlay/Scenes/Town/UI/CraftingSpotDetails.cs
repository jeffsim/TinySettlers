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
            var reservedBy = spot.Data.Reservation.ReservedBy;
            Reservation.text = "Reservation:";
            Reservation.text += "\n    By: " + reservedBy;
            Reservation.text += "\n    Task: " + reservedBy.AI.CurrentTask;
            Reservation.text += "\n    Item: " + reservedBy.AI.CurrentTask.GetTaskItem().Defn.FriendlyName;
        }
        else
            Reservation.text = "Not reserved";

        var str = "Crafting resources:";
        foreach (var resource in spot.Data.Building.Defn.CraftableItems)
            str += "\n    " + resource.Id;
        Item.text = str;
    }
}
