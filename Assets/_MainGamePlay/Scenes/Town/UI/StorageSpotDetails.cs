using System;
using TMPro;
using UnityEngine;

public class StorageSpotDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Item;
    public TextMeshProUGUI Reservation;
    public StorageSpot spot;
    SceneWithMap scene;

    public void ShowForStorageSpot(SceneWithMap scene, StorageSpot spot)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.spot = spot;
        Name.text = "Storage (" + spot.Data.InstanceId + ")";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    void Update()
    {
        if (spot == null) return;

        var str = "Item:\n";
        if (spot.Data.ItemContainer.Item != null)
            str += spot.Data.ItemContainer.Item.DefnId;
        else
            str += "empty";
        Item.text = str + "\n";

        if (spot.Data.Reservation.IsReserved)
        {
            Reservation.text = "Reserved";
            Reservation.text += "\n    By: " + spot.Data.Reservation.ReservedBy;
            Reservation.text += "\n    Task: " + spot.Data.Reservation.ReservedBy.CurrentTask;
            Reservation.text += "\n    Item: " + spot.Data.Reservation.ReservedBy.CurrentTask.GetTaskItem()?.FriendlyName;
        }
        else
            Reservation.text = "Not reserved";
    }
}
