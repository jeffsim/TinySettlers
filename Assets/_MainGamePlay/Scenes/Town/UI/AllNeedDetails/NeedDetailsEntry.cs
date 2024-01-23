using System;
using TMPro;
using UnityEngine;

public class NeedDetailsEntry : MonoBehaviour
{
    public TextMeshProUGUI Priority;
    public TextMeshProUGUI Type;
    public TextMeshProUGUI Source;
    public TextMeshProUGUI Info;
    [NonSerialized] public NeedData Need;

    public void ShowForNeed(NeedData need)
    {
        Need = need;
        Update();
    }

    void Update()
    {
        if (Need == null) return;
        Priority.text = Need.Priority.ToString("0.0");
        Info.text = Need.State.ToString();

        switch (Need.Type)
        {
            case NeedType.ClearStorage:
                Type.text = "Clear Storage";
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.ConstructionWorker:
                Type.text = "Construction Worker";
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.CraftingOrConstructionMaterial:
                Type.text = "Item";// "Need Item: " + need.NeededItem;
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.GatherResource:
                Type.text = "Gather";//(" + need.NeededItem.Id + ")";
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.PersistentRoomNeed:
                Type.text = "Persistent need";
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.PickupAbandonedItem:
                Type.text = "Pickup item";//: " + need.AbandonedItemToPickup;
                Source.text = Need.AbandonedItemToPickup.ToString();
                Info.text = Need.State.ToString();
                break;
            case NeedType.SellGood:
                Type.text = "Sell good";// " + need.NeededItem;
                Source.text = Need.BuildingWithNeed.ToString();
                Info.text = Need.State.ToString();
                break;
            default:
                Debug.LogError("unknown need type " + Need.Type);
                break;
        };
    }
}
