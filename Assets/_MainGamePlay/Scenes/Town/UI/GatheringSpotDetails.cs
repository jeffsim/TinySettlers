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

        if (spot.Data.IsReserved)
        {
            Reservation.text = "Reservation:";
            Reservation.text += "\n    By: " + spot.Data.ReservedBy;
            Reservation.text += "\n    Task: " + spot.Data.ReservedBy.CurrentTask;
            Reservation.text += "\n    Item: " + spot.Data.ReservedBy.CurrentTask.GetTaskItem().FriendlyName;
        }
        else
            Reservation.text = "Not reserved";

        var str = "Gatherable resources:";
        foreach (var resource in spot.Data.Building.Defn.ResourcesThatCanBeGatheredFromHere)
            str += "\n    " + resource.Id;
        Item.text = str;
    }
}
