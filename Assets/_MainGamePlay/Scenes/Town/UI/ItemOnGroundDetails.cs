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

        Reservation.text = "TODO: show reserved details";
    }
}
