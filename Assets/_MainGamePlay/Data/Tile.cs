using System;

[Serializable]
public class TileData : BaseData
{
    private TileDefn _defn;
    public TileDefn Defn
    {
        get
        {
            if (_defn == null)
                _defn = GameDefns.Instance.TileDefns[DefnId];
            return _defn;
        }
    }
    public string DefnId;
    public int TileX;
    public int TileY;

    static float TileSize = 10;

    // Where the Tile is located (== TileLoc * TileSize)
    public float WorldX => TileX * TileSize;
    public float WorldY => TileY * TileSize;

    public BuildingData BuildingInTile;
} 