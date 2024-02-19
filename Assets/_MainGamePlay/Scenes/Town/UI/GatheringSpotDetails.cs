using System;
using TMPro;
using UnityEngine;

public class GatheringSpotDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Item;
    public TextMeshProUGUI Reservation;
    public GatheringSpot spot;
    SceneWithMap scene;

    public void ShowForGatheringSpot(SceneWithMap scene, GatheringSpot spot)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.spot = spot;
        Name.text = "Gathering spot (" + spot.Data.InstanceId + ")";
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

        var str = "Gatherable resources:";
        foreach (var resource in spot.Data.Building.Defn.ResourcesThatCanBeGatheredFromHere)
            str += "\n    " + resource.Id;

        if (spot.Data.ItemContainer.Item != null)
            str += " (grown)";
        else if (spot.Data.ItemGrownInSpotDefnId != null)
            str += " (" + (spot.Data.PercentGrown * 100).ToString("0.0")  + "% grown)";
        Item.text = str;
    }
}
