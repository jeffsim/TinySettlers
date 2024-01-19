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

    static float ItemZ = -7.5f;

    public void Initialize(ItemData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        name = data.DefnId;
        Name.text = data.Defn.FriendlyName.Substring(0, 1);

        GetComponentInChildren<Renderer>().material.color = data.Defn.Color;
        transform.position = new Vector3(data.WorldLocOnGround.x, data.WorldLocOnGround.y, ItemZ);
    }

    public void OnMouseUp()
    {
        Debug.Log(1);
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log(2);

            scene.OnItemOnGroundClicked(this);
        }
    }

}