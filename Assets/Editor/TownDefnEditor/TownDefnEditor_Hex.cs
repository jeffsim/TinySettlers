using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public partial class TownDefnEditor : OdinEditor
{
    // NOTE: Using odd-r Hex coordinate system.  See: https://www.redblobgames.com/grids/hexagons/

    private void showDraggingBuilding_HexTiles()
    {
        if (!GridRect.Contains(Event.current.mousePosition))
            return;

        // Get mouse position relative to rect
        var hexTile = ConvertMousePosToHexTile();
        var text = "(" + hexTile.x + "," + hexTile.y + ")";

        // Draw where the building would drop
        var screenPos = ConvertHexTileToScreenPos(hexTile.x, hexTile.y);

        // Offset screenPos.y by -5 for every other building that is also in hexTile
        int positionInStack = 0;
        foreach (var building in TownDefn.Buildings)
            if (building != draggingBuilding && building.TileX == hexTile.x && building.TileY == hexTile.y)
                positionInStack++;
        screenPos.y -= 5 * positionInStack;

        drawBuildingWithLabelAt(screenPos, draggingBuilding, text);

        // draw dragged building at mouse position
        drawBuildingWithLabelAt(dragMousePosition - dragOffset - TileSizeVector / 2, draggingBuilding, text);
    }

    public struct HexCoord
    {
        public int q, r;
    }

    public struct OddQ
    {
        public int col, row;
    }

    Vector2Int axial_to_oddr(HexCoord hex)
    {
        var col = hex.q + (hex.r - (hex.r & 1)) / 2;
        var row = hex.r;
        return new(col, row);
    }

    HexCoord oddr_to_axial(OddQ oddr)
    {
        var q = oddr.col - (oddr.row - (oddr.row & 1)) / 2;
        var r = oddr.row;
        return new HexCoord { q = q, r = r };
    }

    private Vector2Int ConvertRectPosToHexTile(Vector2 rectPos)
    {
        float tilePlacementWidth = TileSize * 1.5f;
        int hexTileX = (int)(rectPos.x / tilePlacementWidth);
        int hexTileY = (int)(rectPos.y / TileSize) * 2;
        if (rectPos.x > hexTileX * tilePlacementWidth + tilePlacementWidth / 2)
            hexTileY++;
        return new Vector2Int(hexTileX, hexTileY);
    }

    private Vector2 ConvertMousePosToRectPos()
    {
        Vector2 screenPos = Event.current.mousePosition - GridRect.position;
        screenPos.y = GridRect.height - screenPos.y;
        return screenPos;
    }

    private Vector2Int ConvertMousePosToHexTile()
    {
        return ConvertRectPosToHexTile(ConvertMousePosToRectPos());
    }

    private Vector2 ConvertHexTileToScreenPos(int hexTileX, int hexTileY, int positionInStack = 0)
    {
        float screenX, screenY;
        screenX = GridRect.x + hexTileX * TileSize * 1.5f;
        if (hexTileY % 2 == 1)
            screenX += TileSize * 3 / 4f;
        screenY = GridRect.y + (TownDefn.Height - hexTileY / 2f - 2) * TileSize - 5 * positionInStack;

        return new(screenX, screenY);
    }

    private void renderMap_HexTiles()
    {
        float tileSize = Mathf.Min(64, 64);

        var shadowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.black } };
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };

        // Render buildings bottom to top to maintain Z
        var buildings = TownDefn.Buildings.ToArray();
        Array.Sort(buildings, (a, b) => b.TileY.CompareTo(a.TileY));

        foreach (var buildingDefn in buildings)
        {
            if (!buildingDefn.IsEnabled || buildingDefn.Building == null) continue;

            var screenPos = ConvertHexTileToScreenPos(buildingDefn.TileX, buildingDefn.TileY, buildingDefn.PositionInStack);
            drawFilledHexagonAt(screenPos, tileSize, buildingDefn.Building.EditorColor);

            var name = !string.IsNullOrEmpty(buildingDefn.TestId) ? buildingDefn.TestId : buildingDefn.Building.FriendlyName;
            if (name.Length > 2)
            {
                var text = name.Substring(0, Math.Min(name.Length, 6));
                EditorGUI.LabelField(new Rect(screenPos.x + 2, screenPos.y + 1, tileSize - 3, tileSize - 3), text, shadowStyle);
                EditorGUI.LabelField(new Rect(screenPos.x + 1, screenPos.y, tileSize - 3, tileSize - 3), text, style);
                text = "(" + buildingDefn.TileX + "," + buildingDefn.TileY + ")";
                EditorGUI.LabelField(new Rect(screenPos.x + 2, screenPos.y + 16, tileSize - 3, tileSize - 3), text, style);
            }
        }
    }

    void ForceUpdatePositionsInStack()
    {
        int[,] stacks = new int[TownDefn.Width, TownDefn.Height];
        for (int y = 0; y < TownDefn.Height; y++)
            for (int x = 0; x < TownDefn.Width; x++)
                stacks[x, y] = 0;

        foreach (var building in TownDefn.Buildings)
            if (building.IsEnabled)
            {
                if (building.TileX < 0 || building.TileX >= TownDefn.Width || building.TileY < 0 || building.TileY >= TownDefn.Height)
                {
                    Debug.Log("Building " + building.Building.FriendlyName + " is out of bounds at " + building.TileX + "," + building.TileY);
                    continue;
                }
                building.PositionInStack = stacks[building.TileX, building.TileY]++;
            }
    }

    private void drawBuildingWithLabelAt(Vector2 pos, Town_BuildingDefn building, string text)
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };
        drawFilledHexagonAt(pos, TileSize, building.Building.EditorColor);
        EditorGUI.LabelField(new Rect(pos.x + 2, pos.y - 16, TileSize - 3, TileSize - 3), text, style);
    }

    private void drawFilledHexagonAt(Vector2 vec, float tileSize, Color innerColor) => drawFilledHexagonAt(vec.x, vec.y, tileSize, innerColor);
    private void drawFilledHexagonAt(float finalX, float finalY, float tileSize, Color innerColor)
    {
        float centerY = finalY + tileSize / 2;
        float verticalScale = 1;// 0.9f;
        Vector2 p1 = new(finalX + tileSize / 4, centerY - tileSize * verticalScale / 2);
        Vector2 p2 = new(finalX + tileSize * 3 / 4, centerY - tileSize * verticalScale / 2);
        Vector2 p3 = new(finalX + tileSize, centerY);
        Vector2 p4 = new(finalX + tileSize * 3 / 4, centerY + tileSize * verticalScale / 2);
        Vector2 p5 = new(finalX + tileSize / 4, centerY + tileSize * verticalScale / 2);
        Vector2 p6 = new(finalX, centerY);

        Handles.color = innerColor;
        Handles.DrawAAConvexPolygon(p1, p2, p3, p4, p5, p6);
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(2, p1, p2, p3, p4, p5, p6, p1);
    }

    private void HandleMouseInput_HexTiles()
    {
        Event currentEvent = Event.current;

        // Handle Mouse Down - Start Drag
        if (currentEvent.type == EventType.MouseDown && GridRect.Contains(currentEvent.mousePosition))
        {
            Town_BuildingDefn buildingAtTopOfStack = null;
            foreach (var building in TownDefn.Buildings)
            {
                Rect buildingRect = new(ConvertHexTileToScreenPos(building.TileX, building.TileY, building.PositionInStack), TileSizeVector);
                if (buildingRect.Contains(currentEvent.mousePosition))
                    if (buildingAtTopOfStack == null || building.PositionInStack > buildingAtTopOfStack.PositionInStack)
                        buildingAtTopOfStack = building;
            }

            if (buildingAtTopOfStack != null)
            {
                draggingBuilding = buildingAtTopOfStack; // Mark this building as being dragged
                isDragging = true;
                dragMousePosition = currentEvent.mousePosition;
                dragOffset = new(0, 0);
                currentEvent.Use();
            }
        }

        // Handle Mouse Drag - Show Drag Square
        if (currentEvent.type == EventType.MouseDrag && isDragging)
        {
            dragMousePosition = currentEvent.mousePosition;
            currentEvent.Use();
        }

        // Handle Mouse Up - Drop and Record Undo
        if (currentEvent.type == EventType.MouseUp && isDragging)
        {
            // Only record the undo event here, right before the change
            Undo.RecordObject(TownDefn, "Move Building");

            // Calculate new position based on mouse position
            var hexTile = ConvertMousePosToHexTile();

            // Update the building's position
            if (draggingBuilding != null)
            {
                draggingBuilding.TileX = hexTile.x;
                draggingBuilding.TileY = hexTile.y;
                TownDefn.Buildings.Remove(draggingBuilding);
                TownDefn.Buildings.Add(draggingBuilding);
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(TownDefn);
            }

            // Reset drag state
            draggingBuilding = null;
            isDragging = false;
            currentEvent.Use();
        }
    }
}