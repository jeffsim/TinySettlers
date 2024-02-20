using System;
using UnityEngine;

[Serializable]
public class TileData : BaseData, ILocationProvider
{
    private TileDefn _defn;
    public TileDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.TileDefns[DefnId];
    public string DefnId;
    public int TileX;
    public int TileY;
    [SerializeField] public LocationComponent Location { get; set; }

    static float TileSize = 10;
    public float WorldX => Location.WorldLoc.x;
    public float WorldY => Location.WorldLoc.z;

    public BuildingData BuildingInTile;

    public TileData(int x, int y, string defnId)
    {
        Debug.Assert(!String.IsNullOrEmpty(defnId), "null defnId in TileData constructor");
        TileX = x;
        TileY = y;
        DefnId = defnId;
        Location = new LocationComponent(new Vector3(TileX * TileSize, Settings.Current.TileY, TileY * TileSize));
    }
}