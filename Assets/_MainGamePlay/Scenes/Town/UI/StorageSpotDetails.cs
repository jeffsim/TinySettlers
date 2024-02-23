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

        Item.text = "";
        foreach (var spot in pile.Data.StorageSpots)
        {
            if (spot.Container.HasItem)
                Item.text += spot.InstanceId + " item: " + spot.Container.FirstItem.DefnId + "\n";
            if (spot.Reservable.IsReserved)
                Item.text += spot.InstanceId + " reserved by: " + spot.Reservable.ReservedBy + "\n";
        }
    }
}
