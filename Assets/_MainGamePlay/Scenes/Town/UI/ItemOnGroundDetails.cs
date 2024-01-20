using TMPro;
using UnityEngine;

public class ItemOnGroundDetails : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Reservation;
    public Item item;
    SceneWithMap scene;

    public void ShowForItemOnGround(SceneWithMap scene, Item item)
    {
        gameObject.SetActive(true);
        this.scene = scene;
        this.item = item;
        Name.text = "Item on ground (" + item.Data.InstanceId + ")";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (item == null) return;

        // Get the town need that tracks the need to pick up this item
        var need = scene.Map.Town.otherTownNeeds.Find(n => n.Type == NeedType.PickupAbandonedItem && n.AbandonedItemToPickup == item.Data);
        if (need == null)
        {
            Reservation.text = "ERROR: Failed to find need";
            return;
        }

        Reservation.text = "  Priority: " + need.Priority +
                           "\n  Start time:" + need.StartTimeInSeconds;
                           

        if (need.IsBeingFullyMet)
            Reservation.text += "\n  Being met by worker " + need.WorkersMeetingNeed[0].InstanceId;
        else
            Reservation.text += "\n  Not being met";
    }
}
