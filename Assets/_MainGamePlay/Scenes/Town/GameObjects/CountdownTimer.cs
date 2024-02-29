using System;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    public GameObject TimerImage;
    public SpriteRenderer IconImage;
    public Sprite UnderConstructionImage;

    [NonSerialized] BuildingData building;

    public void InitializeForBuilding(BuildingData building)
    {
        this.building = building;
        building.Generatable.OnPercentChanged += OnPercentChanged;
        OnCraftedGoodChanged(building);
    }

    void OnDestroy()
    {
        building.Generatable.OnPercentChanged -= OnPercentChanged;
    }

    void OnPercentChanged()
    {
        //    gameObject.SetActive(!building.ConstructionMgr.IsConstructed);
        // IconImage.sprite = UnderConstructionImage;
    }

    void OnCraftedGoodChanged(BuildingData building)
    {
        // var isCrafting = building != null && building.CraftingMgr.IsCraftingGood;
        // gameObject.SetActive(isCrafting);
        // if (isCrafting)
        // {
        //     var good = GameDefns.Instance.GoodDefns[building.CraftingMgr.CurrentItemBeingCraftedDefnId];
        //     IconImage.sprite = good.SpriteToShowInTimer;
        // }
    }

    private void Update()
    {
        if (!building.Generatable.IsEnabled) return;
        var value = building.Generatable.PercentGenerated;

        // get TimerImage's meshrenderer's material and set the _FillAmount property to value
        TimerImage.GetComponent<MeshRenderer>().material.SetFloat("_FillAmount", value);
    }
}
