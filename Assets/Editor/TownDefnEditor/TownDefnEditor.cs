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
            var gridPos = ConvertMousePosToGridPos();
            Debug.Log(gridPos + ", " + gridPos / 64f + ", " + (gridPos + new Vector2(32, 32)) / 64f);
            Repaint();
        }

        ForceUpdatePositionsInStack();
    }
}