using System;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

public partial class TownDefnEditor : OdinEditor
{
    // NOTE: Using odd-Q Hex coordinate system.  See: https://www.redblobgames.com/grids/hexagons/#coordinates-offset
    // “odd-q” vertical layout shoves odd columns down
    private void showDraggingBuilding_HexTiles()
    {
        if (!GridRect.Contains(Event.current.mousePosition))
            return;

        // Get mouse position relative to rect
        var hexTile = ConvertMousePosToHexTile();
        var text = "(" + hexTile.x + "," + hexTile.y + ")";

        // Draw where the building would drop
        var rectPos = ConvertHexTileToRectPos(hexTile);

        // Offset screenPos.y by -5 for every other building that is also in hexTile
        int positionInStack = 0;
        foreach (var building in TownDefn.Buildings)
            if (building != draggingBuilding && building.TileX == hexTile.x && building.TileY == hexTile.y)
                positionInStack++;
        rectPos.y -= 5 * positionInStack;

        drawBuildingWithLabelAt(rectPos, draggingBuilding, text);

        // draw dragged building at mouse position
        drawBuildingWithLabelAt(dragMousePosition - dragOffset - TileSizeVector / 2, draggingBuilding, text);
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

    private Vector2Int ConvertRectPosToHexTile(Vector2 rectPos)
    {
        float hexTileX = rectPos.x / (TileSize * 3 / 4f);
        float hexTileY = rectPos.y / TileSize - ((int)hexTileX & 1) * 0.5f;
        return new Vector2Int((int)hexTileX, (int)hexTileY);
    }

    private Vector2 ConvertHexTileToRectPos(Vector2Int hexTile, int positionInStack = 0)
    {
        float rectX = hexTile.x * TileSize * 3 / 4f;
        float rectY = (TownDefn.Height - hexTile.y - .5f - (hexTile.x & 1) / 2f) * TileSize - 5 * positionInStack;
        return GridRect.position + new Vector2(rectX, rectY);
    }

    private void drawDebugTiles()
    {
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = new Color(.4f, .4f, .4f, 1) } };

        var tileColor = new Color(0.2f, 0.2f, 0.2f, 1);
        var borderColor = new Color(0.15f, 0.15f, 0.15f, 1);
        for (int y = 0; y < TownDefn.Height; y++)
            for (int x = 0; x < TownDefn.Width; x++)
            {
                var screenPos = ConvertHexTileToRectPos(new(x, y));
                drawFilledHexagonAt(screenPos, TileSize, tileColor, borderColor);
                EditorGUI.LabelField(new(screenPos, TileSizeVector - Vector2.one * 3), "(" + x + "," + y + ")", style);
            }

        // draw box at mouse coords
        // var rectPos = ConvertMousePosToRectPos();
        // var mousePos = new Vector2(GridRect.x + rectPos.x, GridRect.y + GridRect.height - rectPos.y);
        // EditorGUI.DrawRect(new Rect(mousePos.x - 4, mousePos.y - 4, 8, 8), Color.red);

        // // draw line from mousePos to closest Hex center
        // var screenPos2 = ConvertHexTileToRectPos(ConvertRectPosToHexTile(rectPos)) + TileSizeVector / 2;
        // EditorGUI.DrawRect(new Rect(screenPos2.x - 4, screenPos2.y - 4, 8, 8), Color.blue);
        // Handles.color = Color.blue;
        // Handles.DrawAAPolyLine(2, mousePos, screenPos2);
    }

    private void renderMap_HexTiles()
    {
        var shadowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.black } };
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };

        drawDebugTiles();

        // Sort buildings bottom to top to render Z order correctly
        var buildings = TownDefn.Buildings.ToArray();
        Array.Sort(buildings, (a, b) => b.TileY.CompareTo(a.TileY));

        foreach (var buildingDefn in buildings)
        {
            if (!buildingDefn.IsEnabled || buildingDefn.Building == null) continue;

            var screenPos = ConvertHexTileToRectPos(new(buildingDefn.TileX, buildingDefn.TileY), buildingDefn.PositionInStack);
            var color = buildingDefn.Building.EditorColor;
            if (buildingDefn == draggingBuilding)
                color /= 2;
            drawFilledHexagonAt(screenPos, TileSize, color, Color.black);

            var name = !string.IsNullOrEmpty(buildingDefn.TestId) ? buildingDefn.TestId : buildingDefn.Building.FriendlyName;
            if (name.Length > 2)
            {
                var text = name.Substring(0, Math.Min(name.Length, 6));
                EditorGUI.LabelField(new Rect(screenPos.x + 2, screenPos.y + 1, TileSize - 3, TileSize - 3), text, shadowStyle);
                EditorGUI.LabelField(new Rect(screenPos.x + 1, screenPos.y, TileSize - 3, TileSize - 3), text, style);
                text = "(" + buildingDefn.TileX + "," + buildingDefn.TileY + ")";
                EditorGUI.LabelField(new Rect(screenPos.x + 2, screenPos.y + 16, TileSize - 3, TileSize - 3), text, style);
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
        drawFilledHexagonAt(pos, TileSize, building.Building.EditorColor, Color.black);
        EditorGUI.LabelField(new Rect(pos.x + 2, pos.y - 16, TileSize - 3, TileSize - 3), text, style);
    }

    private void drawFilledHexagonAt(Vector2 vec, float tileSize, Color innerColor, Color borderColor) => drawFilledHexagonAt(vec.x, vec.y, tileSize, innerColor, borderColor);
    private void drawFilledHexagonAt(float finalX, float finalY, float tileSize, Color innerColor, Color borderColor)
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
        Handles.color = borderColor;
        Handles.DrawAAPolyLine(2, p1, p2, p3, p4, p5, p6, p1);
    }
    bool movedEnoughToDrag;
    private void HandleMouseInput_HexTiles()
    {
        Event currentEvent = Event.current;
        var hexTile = ConvertMousePosToHexTile();

        // Handle Mouse Down - Start Drag
        if (currentEvent.type == EventType.MouseDown && GridRect.Contains(currentEvent.mousePosition))
        {
            Town_BuildingDefn buildingAtTopOfStack = null;
            foreach (var building in TownDefn.Buildings)
            {
                if (building.IsEnabled && building.TileX == hexTile.x && building.TileY == hexTile.y)
                    if (buildingAtTopOfStack == null || building.PositionInStack > buildingAtTopOfStack.PositionInStack)
                        buildingAtTopOfStack = building;
            }

            if (buildingAtTopOfStack != null)
            {
                draggingBuilding = buildingAtTopOfStack; // Mark this building as being dragged
                isDragging = true;
                movedEnoughToDrag = false;
                dragMousePosition = dragStart = currentEvent.mousePosition;
                dragOffset = new(0, 0);
                currentEvent.Use();
            }
        }

        // Handle Mouse Drag - Show Drag Square
        if (currentEvent.type == EventType.MouseDrag && isDragging)
        {
            dragMousePosition = currentEvent.mousePosition;
            movedEnoughToDrag |= Vector2.Distance(dragMousePosition, dragStart) > 5;
            currentEvent.Use();
        }

        // Handle Mouse Up - Drop and Record Undo
        if (currentEvent.type == EventType.MouseUp && isDragging)
        {
            if (movedEnoughToDrag)
            {
                // Update the building's position
                if (draggingBuilding != null)
                {
                    // Record the undo event here, right before the change
                    Undo.RecordObject(TownDefn, "Move Building");

                    draggingBuilding.TileX = hexTile.x;
                    draggingBuilding.TileY = hexTile.y;
                    TownDefn.Buildings.Remove(draggingBuilding);
                    TownDefn.Buildings.Add(draggingBuilding);
                    Undo.FlushUndoRecordObjects();
                    EditorUtility.SetDirty(TownDefn);
                }
            }
            else
            {
                // Clicked; duplicate building in tile
                Undo.RecordObject(TownDefn, "Duplicate Building");

                var newBuilding = new Town_BuildingDefn()
                {
                    IsEnabled = true,
                    Building = draggingBuilding.Building,
                    TileX = hexTile.x,
                    TileY = hexTile.y,
                    PositionInStack = 0,
                    StartingItemsInBuilding = new(),
                    NumWorkersStartAtBuilding = 0
                };
                TownDefn.Buildings.Add(newBuilding);

                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(TownDefn);
            }
            // Reset drag state
            draggingBuilding = null;
            isDragging = false;
            currentEvent.Use();
        }
    }

    private Town_BuildingDefn getBuildingAt(Vector2Int hexTile)
    {
        foreach (var building in TownDefn.Buildings)
            if (building.IsEnabled && building.TileX == hexTile.x && building.TileY == hexTile.y)
                return building;
        return null;
    }
}