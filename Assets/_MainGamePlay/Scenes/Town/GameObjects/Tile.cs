using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [NonSerialized] public TileData Data;

    public GameObject Visual;
    public SceneWithMap scene;


    public void Initialize(TileData data, SceneWithMap scene)
    {
        this.scene = scene;
        Data = data;
        name = "Tile " + data.TileX + " " + data.TileY;

        if (Settings.AllowFreeBuildingPlacement)
            gameObject.RemoveAllChildren();
        else
            GetComponentInChildren<Renderer>().material = data.Defn.TileColor;

        transform.position = data.Location.WorldLoc;
        // Hack
        if (Settings.UseOrthographicCamera)
            transform.position += new Vector3(0, 0, .5f);
    }

    public void OnClicked()
    {
        // if mouse is still over this tile then call scene.OnTileClicked
        if (scene.Map.getTileAt(Input.mousePosition) == this)
            scene.OnTileClicked(this);
    }
}