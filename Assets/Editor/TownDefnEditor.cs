using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TownDefn))]
public class TownDefnEditor : OdinEditor
{
    GameDefnsMgr myGameDefns;
    private Town_BuildingDefn draggingBuilding = null;
    private bool isDragging = false;
    float tileSize = 64;
    Vector2 tileSizeVector = new(64, 64);
    private Vector2 dragMousePosition;
    bool UseHexTiles;
    Vector2 dragOffset;

    public override void OnInspectorGUI()
    {
        TownDefn mapDefn = (TownDefn)Selection.activeObject;

        DrawDefaultInspector();

        // TODO: Horribly inefficient; but only impacts Editor and doesn't impact perf (for now)
        myGameDefns = new GameDefnsMgr();
        myGameDefns.RefreshDefns();
        UseHexTiles = myGameDefns.GameSettingsDefns["default"].HexTiles;

        // render map
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        var width = mapDefn.Width * tileSize + tileSize / 2;
        var height = mapDefn.Height * tileSize - tileSize;
        Rect gridRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        var borderRect = gridRect.Expand(2, 2);
        GUI.Box(borderRect, "");
        if (UseHexTiles)
            renderMap_HexTiles(gridRect, mapDefn);
        else
            renderMap_GridTiles(gridRect, mapDefn);
        GUILayout.EndHorizontal();

        // handle drag
        if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
        {
            if (UseHexTiles)
                HandleMouseInput_HexTiles(mapDefn, gridRect);
            else
                HandleMouseInput_GridTiles(mapDefn, gridRect);
        }

        // Show dragsquare
        if (Event.current.type == EventType.Repaint && isDragging)
        {
            if (UseHexTiles)
                showDraggingBuilding_HexTiles(mapDefn, gridRect);
            else
                showDraggingBuilding_GridTiles(mapDefn, gridRect);
            Repaint();
        }

        ForceUpdatePositionsInStack(mapDefn);
    }

    private void showDraggingBuilding_HexTiles(TownDefn mapDefn, Rect rect)
    {
        if (!rect.Contains(Event.current.mousePosition))
            return;

        // Get mouse position relative to rect
        var hexTile = ConvertMousePosToHexTile(rect);
        var text = "(" + hexTile.x + "," + hexTile.y + ")";

        // draw where the building would drop
        drawBuildingWithLabelAt(ConvertHexTileToScreenPos(rect, mapDefn, hexTile.x, hexTile.y), draggingBuilding, text);

        // drag dragged building at mouse position
        drawBuildingWithLabelAt(dragMousePosition - dragOffset - tileSizeVector / 2, draggingBuilding, text);
    }

    private Vector2Int ConvertMousePosToHexTile(Rect rect)
    {
        Vector2 screenPos = Event.current.mousePosition - rect.position;
        screenPos.y = rect.height - screenPos.y;
        return ConvertScreenPosToHexTile(screenPos);
    }

    private Vector2Int ConvertScreenPosToHexTile(Vector2 screenPos)
    {
        int hexTileX = Mathf.FloorToInt(screenPos.x / (tileSize * 1.5f));
        int hexTileY = Mathf.FloorToInt(screenPos.y / tileSize) * 2;
        if (screenPos.x > hexTileX * tileSize * 1.5f + tileSize * 3 / 4)
       //     if (screenPos.y > (hexTileY / 2) * tileSize)
                hexTileY++;
        return new(hexTileX, hexTileY);
    }

    private Vector2 ConvertHexTileToScreenPos(Rect rect, TownDefn mapDefn, int hexTileX, int hexTileY, int positionInStack = 0)
    {
        float screenX, screenY;
        screenX = rect.x + hexTileX * tileSize * 1.5f;
        if (hexTileY % 2 == 1)
            screenX += tileSize * 3 / 4f;
        screenY = rect.y + (mapDefn.Height - hexTileY / 2f - 2) * tileSize - 5 * positionInStack;

        return new(screenX, screenY);
    }

    private void renderMap_HexTiles(Rect rect, TownDefn mapDefn)
    {
        float tileSize = Mathf.Min(64, 64);

        var shadowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.black } };
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };

        foreach (var buildingDefn in mapDefn.Buildings)
        {
            if (!buildingDefn.IsEnabled || buildingDefn.Building == null) continue;

            var screenPos = ConvertHexTileToScreenPos(rect, mapDefn, buildingDefn.TileX, buildingDefn.TileY, buildingDefn.PositionInStack);
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

    void ForceUpdatePositionsInStack(TownDefn mapDefn)
    {
        int[,] stacks = new int[mapDefn.Width, mapDefn.Height];
        for (int y = 0; y < mapDefn.Height; y++)
            for (int x = 0; x < mapDefn.Width; x++)
                stacks[x, y] = 0;

        foreach (var building in mapDefn.Buildings)
            if (building.IsEnabled)
            {
                if (building.TileX < 0 || building.TileX >= mapDefn.Width || building.TileY < 0 || building.TileY >= mapDefn.Height)
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
        drawFilledHexagonAt(pos, tileSize, building.Building.EditorColor);
        EditorGUI.LabelField(new Rect(pos.x + 2, pos.y - 16, tileSize - 3, tileSize - 3), text, style);
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

    private void renderMap_GridTiles(Rect rect, TownDefn mapDefn)
    {
        float tileSize = Mathf.Min(64, 64);

        var shadowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.black } };
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };
        var whiteStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } };

        string[] tiles = mapDefn.Tiles.Split(",");
        Debug.Assert(tiles.Length == mapDefn.Width * mapDefn.Height, "wrong num tiles");
        for (int y = 0; y < mapDefn.Height; y++)
            for (int x = 0; x < mapDefn.Width; x++)
            {
                var finalX = rect.x + x * tileSize;
                var finalY = rect.y + y * tileSize;
                var tileId = tiles[y * mapDefn.Width + x];
                Debug.Assert(myGameDefns.TileDefns.ContainsKey(tileId), tileId + "not in tiledefns");
                var tile = myGameDefns.TileDefns[tileId];
                var name = tile.FriendlyName.Substring(0, Math.Min(tile.FriendlyName.Length, 8));
                EditorGUI.DrawRect(new Rect(finalX, finalY, tileSize - 1, tileSize - 1), tile.EditorColor);
                EditorGUI.LabelField(new Rect(finalX + 1, finalY + 1, tileSize - 3, tileSize - 3), name, shadowStyle);
                EditorGUI.LabelField(new Rect(finalX, finalY, tileSize - 3, tileSize - 3), name, whiteStyle);
            }

        foreach (var buildingDefn in mapDefn.Buildings)
        {
            if (!buildingDefn.IsEnabled || buildingDefn.Building == null) continue;
            float finalX, finalY;

            finalX = rect.x + buildingDefn.TileX * tileSize;
            finalY = rect.y + ((mapDefn.Height - 1) * tileSize - buildingDefn.TileY * tileSize);
            var buildingRect = new Rect(finalX + 2, finalY + 2, tileSize - 1 - 4, tileSize - 1 - 4);
            EditorGUI.DrawRect(buildingRect, buildingDefn.Building.EditorColor);

            var name = !string.IsNullOrEmpty(buildingDefn.TestId) ? buildingDefn.TestId : buildingDefn.Building.FriendlyName;
            if (name.Length > 2)
            {
                var text = name.Substring(0, Math.Min(name.Length, 6));
                EditorGUI.LabelField(new Rect(finalX + 2, finalY + 1, tileSize - 3, tileSize - 3), text, shadowStyle);
                EditorGUI.LabelField(new Rect(finalX + 1, finalY, tileSize - 3, tileSize - 3), text, style);
            }
        }
    }

    private void HandleMouseInput_HexTiles(TownDefn mapDefn, Rect gridRect)
    {
        Event currentEvent = Event.current;

        // Handle Mouse Down - Start Drag
        if (currentEvent.type == EventType.MouseDown && gridRect.Contains(currentEvent.mousePosition))
        {
            Town_BuildingDefn buildingAtTopOfStack = null;
            foreach (var building in mapDefn.Buildings)
            {
                Rect buildingRect = new(ConvertHexTileToScreenPos(gridRect, mapDefn, building.TileX, building.TileY, building.PositionInStack), tileSizeVector);
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
            Undo.RecordObject(mapDefn, "Move Building");

            // Calculate new position based on mouse position
            var hexTile = ConvertMousePosToHexTile(gridRect);

            // Update the building's position
            if (draggingBuilding != null)
            {
                draggingBuilding.TileX = hexTile.x;
                draggingBuilding.TileY = hexTile.y;
                mapDefn.Buildings.Remove(draggingBuilding);
                mapDefn.Buildings.Add(draggingBuilding);
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(mapDefn);
            }

            // Reset drag state
            draggingBuilding = null;
            isDragging = false;
            currentEvent.Use();
        }
    }

    private void HandleMouseInput_GridTiles(TownDefn mapDefn, Rect gridRect)
    {
        Event currentEvent = Event.current;
        Vector2 relativePosition = currentEvent.mousePosition - new Vector2(gridRect.x, gridRect.y);

        // Handle Mouse Down - Start Drag
        if (currentEvent.type == EventType.MouseDown && gridRect.Contains(currentEvent.mousePosition))
        {
            foreach (var building in mapDefn.Buildings)
            {
                var tileX = building.TileX * tileSize;
                var tileY = (mapDefn.Height - 1 - building.TileY) * tileSize; // Assuming Y is inverted
                var buildingRect = new Rect(gridRect.x + tileX, gridRect.y + tileY, tileSize, tileSize);
                var xMouseDiffFromTileCenter = currentEvent.mousePosition.x - buildingRect.center.x;
                var yMouseDiffFromTileCenter = currentEvent.mousePosition.y - buildingRect.center.y;
                if (buildingRect.Contains(currentEvent.mousePosition))
                {
                    draggingBuilding = building; // Mark this building as being dragged
                    isDragging = true;
                    dragMousePosition = currentEvent.mousePosition;
                    dragOffset = new(xMouseDiffFromTileCenter, yMouseDiffFromTileCenter);
                    currentEvent.Use();
                    break; // Stop checking other buildings
                }
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
            Undo.RecordObject(mapDefn, "Move Building");

            // Calculate new position based on mouse position
            int newTileX = Mathf.FloorToInt(relativePosition.x / tileSize);
            int newTileY = Mathf.FloorToInt(relativePosition.y / tileSize);

            // Update the building's position
            if (draggingBuilding != null)
            {
                draggingBuilding.TileX = newTileX;
                draggingBuilding.TileY = mapDefn.Height - 1 - newTileY;
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(mapDefn);
            }

            // Reset drag state
            draggingBuilding = null;
            isDragging = false;
            currentEvent.Use();
        }
    }

    private void showDraggingBuilding_GridTiles(TownDefn mapDefn, Rect gridRect)
    {
        // If the mouse cursor is over a gridtile then render a yellow faded square in the tile to hint that dropping would place the building there
        Vector2 relativePosition = Event.current.mousePosition - new Vector2(gridRect.x, gridRect.y);
        int newTileX = Mathf.FloorToInt(relativePosition.x / tileSize);
        int newTileY = Mathf.FloorToInt(relativePosition.y / tileSize);
        if (newTileX >= 0 && newTileX < mapDefn.Width && newTileY >= 0 && newTileY < mapDefn.Height)
        {
            var rect = new Rect(gridRect.x + newTileX * tileSize, gridRect.y + newTileY * tileSize, tileSize, tileSize);
            EditorGUI.DrawRect(rect.Expand(-2, -2), new(1, 1, 0, 0.5f));
        }

        Rect dragSquare = new(dragMousePosition.x - tileSize / 2 - dragOffset.x, dragMousePosition.y - tileSize / 2 - dragOffset.y, tileSize, tileSize);
        EditorGUI.DrawRect(dragSquare, draggingBuilding.Building.EditorColor);
    }

}