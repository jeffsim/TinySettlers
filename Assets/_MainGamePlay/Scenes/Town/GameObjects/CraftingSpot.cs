using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingSpot : MonoBehaviour
{
    public GameObject ReservedIndicator;
    [NonSerialized] public CraftingSpotData Data;

    int index;
    public SceneWithMap scene;
    
    internal void Initialize(CraftingSpotData spot, int index, Building building, SceneWithMap scene)
    {
        this.Data = spot;
        this.index = index;
        this.scene = scene;
        name = "Crafting Spot " + index;
        transform.position = spot.Location.WorldLoc;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnCraftingSpotClicked(this);
    }

    void Update()
    {
        Debug.Assert(Data != null, "null spot");
        ReservedIndicator.SetActive(Data.Reservable.IsReserved);
    }
}
