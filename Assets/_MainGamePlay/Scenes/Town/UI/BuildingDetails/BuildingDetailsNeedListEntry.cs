using System;
using TMPro;
using UnityEngine;

public class BuildingDetailsNeedListEntry : MonoBehaviour
{
    public TextMeshProUGUI Priority;
    public TextMeshProUGUI Type;
    public TextMeshProUGUI Info;
    [NonSerialized] public NeedData Need;

    public void ShowForNeed(NeedData need)
    {
        Need = need;
        Update();
    }

    void Update()
    {
        Priority.text = Need.Priority.ToString("0.0");
        Info.text = Need.State.ToString();

        switch (Need.Type)
        {
            case NeedType.ClearStorage:
                Type.text = "Clear Storage";
                Info.text = Need.State.ToString();
                break;
            case NeedType.ConstructionWorker:
                Type.text = "Construction Worker";
                Info.text = Need.State.ToString();
                break;
            case NeedType.CraftingOrConstructionMaterial:
                Type.text = $"Item ({Need.NeededItem})";
                Info.text = Need.State.ToString();
                break;
            case NeedType.GatherResource:
                Type.text = $"Gather ({Need.NeededItem})";
                Info.text = Need.State.ToString();
                break;
            case NeedType.PersistentBuildingNeed:
                Type.text = "Persistent need";
                Info.text = Need.State.ToString();
                break;
            case NeedType.PickupAbandonedItem:
                Type.text = $"Pickup Item ({Need.AbandonedItemToPickup})";
                Info.text = Need.State.ToString();
                break;
            case NeedType.SellGood:
                Type.text = $"Sell good ({Need.NeededItem})";
                Info.text = Need.State.ToString();
                break;
            case NeedType.CraftGood:
                Type.text = $"Craft good ({Need.NeededItem})";
                Info.text = Need.State.ToString();
                break;
            default:
                Debug.LogError("unknown need type " + Need.Type);
                break;
        };
    }
}
