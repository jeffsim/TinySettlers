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
            Reservation.text = "Reservation:";
            Reservation.text += "\n    By: " + spot.Data.Reservation.ReservedBy;
            Reservation.text += "\n    Task: " + spot.Data.Reservation.ReservedBy.CurrentTask;
            Reservation.text += "\n    Item: " + spot.Data.Reservation.ReservedBy.CurrentTask.GetTaskItem().FriendlyName;
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
