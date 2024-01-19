using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [NonSerialized] public TileData Data;

    public GameObject Visual;
    public SceneWithMap scene;

    static float TileZ = 5;

    public void Initialize(TileData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        name = "Tile " + data.TileX + " " + data.TileY;

        GetComponentInChildren<Renderer>().material = data.Defn.TileColor;
        transform.position = new Vector3(data.WorldX, data.WorldY, TileZ);
    }

    public void OnClicked() => scene.OnTileClicked(this);
}