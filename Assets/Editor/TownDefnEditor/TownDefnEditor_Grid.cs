using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

public partial class TownDefnEditor : OdinEditor
{
    private void renderMap_GridTiles()
    {
        float tileSize = Mathf.Min(64, 64);

        var shadowStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.black } };
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.yellow } };
        var whiteStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } };

        string[] tiles = TownDefn.Tiles.Split(",");
        Debug.Assert(tiles.Length == TownDefn.Width * TownDefn.Height, "wrong num tiles");
        for (int y = 0; y < TownDefn.Height; y++)
            for (int x = 0; x < TownDefn.Width; x++)
            {
                var finalX = GridRect.x + x * tileSize;
                var finalY = GridRect.y + y * tileSize;
                var tileId = tiles[y * TownDefn.Width + x];
                Debug.Assert(myGameDefns.TileDefns.ContainsKey(tileId), tileId + "not in tiledefns");
                var tile = myGameDefns.TileDefns[tileId];
                var name = tile.FriendlyName.Substring(0, Math.Min(tile.FriendlyName.Length, 8));
                EditorGUI.DrawRect(new Rect(finalX, finalY, tileSize - 1, tileSize - 1), tile.EditorColor);
                EditorGUI.LabelField(new Rect(finalX + 1, finalY + 1, tileSize - 3, tileSize - 3), name, shadowStyle);
                EditorGUI.LabelField(new Rect(finalX, finalY, tileSize - 3, tileSize - 3), name, whiteStyle);
            }

        foreach (var buildingDefn in TownDefn.Buildings)
        {
            if (!buildingDefn.IsEnabled || buildingDefn.Building == null) continue;
            float finalX, finalY;

            finalX = GridRect.x + buildingDefn.TileX * tileSize;
            finalY = GridRect.y + ((TownDefn.Height - 1) * tileSize - buildingDefn.TileY * tileSize);
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

    private void HandleMouseInput_GridTiles()
    {
        Event currentEvent = Event.current;
        Vector2 relativePosition = currentEvent.mousePosition - new Vector2(GridRect.x, GridRect.y);

        // Handle Mouse Down - Start Drag
        if (currentEvent.type == EventType.MouseDown && GridRect.Contains(currentEvent.mousePosition))
        {
            foreach (var building in TownDefn.Buildings)
            {
                var tileX = building.TileX * TileSize;
                var tileY = (TownDefn.Height - 1 - building.TileY) * TileSize; // Assuming Y is inverted
                var buildingRect = new Rect(GridRect.x + tileX, GridRect.y + tileY, TileSize, TileSize);
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
            Undo.RecordObject(TownDefn, "Move Building");

            // Calculate new position based on mouse position
            int newTileX = Mathf.FloorToInt(relativePosition.x / TileSize);
            int newTileY = Mathf.FloorToInt(relativePosition.y / TileSize);

            // Update the building's position
            if (draggingBuilding != null)
            {
                draggingBuilding.TileX = newTileX;
                draggingBuilding.TileY = TownDefn.Height - 1 - newTileY;
                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(TownDefn);
            }

            // Reset drag state
            draggingBuilding = null;
            isDragging = false;
            currentEvent.Use();
        }
    }

    private void showDraggingBuilding_GridTiles()
    {
        // If the mouse cursor is over a gridtile then render a yellow faded square in the tile to hint that dropping would place the building there
        Vector2 relativePosition = Event.current.mousePosition - new Vector2(GridRect.x, GridRect.y);
        int newTileX = Mathf.FloorToInt(relativePosition.x / TileSize);
        int newTileY = Mathf.FloorToInt(relativePosition.y / TileSize);
        if (newTileX >= 0 && newTileX < TownDefn.Width && newTileY >= 0 && newTileY < TownDefn.Height)
        {
            var rect = new Rect(GridRect.x + newTileX * TileSize, GridRect.y + newTileY * TileSize, TileSize, TileSize);
            EditorGUI.DrawRect(rect.Expand(-2, -2), new(1, 1, 0, 0.5f));
        }

        Rect dragSquare = new(dragMousePosition.x - TileSize / 2 - dragOffset.x, dragMousePosition.y - TileSize / 2 - dragOffset.y, TileSize, TileSize);
        EditorGUI.DrawRect(dragSquare, draggingBuilding.Building.EditorColor);
    }

}