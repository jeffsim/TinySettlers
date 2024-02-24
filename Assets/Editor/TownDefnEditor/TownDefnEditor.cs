using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TownDefn))]
public partial class TownDefnEditor : OdinEditor
{
    TownDefn TownDefn;

    GameDefnsMgr myGameDefns;

    const float TileSize = 64;

    Vector2 TileSizeVector;
    bool UseHexTiles;
    Rect GridRect;

    // Dragging
    private Town_BuildingDefn draggingBuilding = null;
    private bool isDragging = false;
    Vector2 dragOffset;
    private Vector2 dragMousePosition;

    public override void OnInspectorGUI()
    {
        TownDefn = (TownDefn)Selection.activeObject;

        TileSizeVector = new(TileSize, TileSize);

        DrawDefaultInspector();

        myGameDefns = new GameDefnsMgr();
        myGameDefns.RefreshDefns();
        UseHexTiles = myGameDefns.GameSettingsDefns["default"].HexTiles;

        // render map
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        var width = TownDefn.Width * TileSize + TileSize / 2;
        var height = TownDefn.Height * TileSize - TileSize;
        GridRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        var borderRect = GridRect.Expand(2, 2);
        GUI.Box(borderRect, "");
        if (UseHexTiles)
            renderMap_HexTiles();
        else
            renderMap_GridTiles();

        GUILayout.EndHorizontal();

        // handle drag
        if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
        {
            if (UseHexTiles)
                HandleMouseInput_HexTiles();
            else
                HandleMouseInput_GridTiles();
        }

        // Show dragged building (if any)
        if (Event.current.type == EventType.Repaint && isDragging)
        {
            if (UseHexTiles)
                showDraggingBuilding_HexTiles();
            else
                showDraggingBuilding_GridTiles();
            Repaint();
        }

        if (Event.current.type == EventType.Repaint)
        {
            // var screenPos = ConvertMousePosToRectPos();
            // var tilePos = new Vector2Int((int)(screenPos.x / TileSize), (int)(screenPos.y / TileSize));
            // var hex = GridToHex(tilePos);
            // Debug.Log(tilePos.x + ", " + tilePos.y + " : " + hex.x + ", " + hex.y);
            // Repaint();
        }

        ForceUpdatePositionsInStack();
    }

    Vector2Int GridToHex(Vector2Int gridPos)
    {
        float x = 3 / 2 * gridPos.x;
        float y = Mathf.Sqrt(3f) * (gridPos.y + 0.5f * (gridPos.x & 1));
        return new Vector2Int((int)x, (int)y);
    }

}