using System;
using TMPro;
using UnityEngine;

public class StorageSpotDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Item;
    public TextMeshProUGUI Reservation;
    public StoragePile pile;
    SceneWithMap scene;

    public void ShowForStoragePile(SceneWithMap scene, StoragePile pile)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.pile = pile;
        Name.text = "Storage (" + pile.Data.InstanceId + ")";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }


    void Update()
    {
        if (pile == null) return;

        Item.text = "TODO; pile details";
        // var str = "Item:\n";
        // if (pile.Data.ItemContainer.Item != null)
        //     str += pile.Data.ItemContainer.Item.DefnId;
        // else
        //     str += "empty";
        // Item.text = str + "\n";

        // if (pile.Data.Reservation.IsReserved)
        // {
        //     Reservation.text = "Reserved";
        //     Reservation.text += "\n    By: " + pile.Data.Reservation.ReservedBy;
        //     Reservation.text += "\n    Task: " + pile.Data.Reservation.ReservedBy.CurrentTask;
        //     Reservation.text += "\n    Item: " + pile.Data.Reservation.ReservedBy.CurrentTask.GetTaskItem()?.FriendlyName;
        // }
        // else
        //     Reservation.text = "Not reserved";
    }
}
