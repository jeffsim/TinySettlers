using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Item : MonoBehaviour
{
    [NonSerialized] public ItemData Data;

    public TextMeshPro Name;
    public GameObject Visual;
    public SceneWithMap scene;

    public void Initialize(ItemData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        name = data.DefnId;
        Name.text = data.Defn.FriendlyName.Substring(0, 1);

        // GetComponentInChildren<Renderer>().material.color = data.Defn.Color;
        transform.position = data.Location.WorldLoc;

        if (Data.Defn.VisualPrefab != null)
        {
            var visual = Instantiate(Data.Defn.VisualPrefab);
            visual.transform.SetParent(Visual.transform, false);
            // buildingVisual.transform.localPosition = Data.Defn.VisualOffset;
            // buildingVisual.transform.localScale = Data.Defn.VisualScale;
            // buildingVisual.transform.localRotation = Data.Defn.VisualRotation;
        }
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            scene.OnItemOnGroundClicked(this);
    }
}