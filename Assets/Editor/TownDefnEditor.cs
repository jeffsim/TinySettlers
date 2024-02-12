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
    private Vector2 dragMousePosition;

    public override void OnInspectorGUI()
    {
        TownDefn mapDefn = (TownDefn)Selection.activeObject;

        DrawDefaultInspector();

        // TODO: Horribly inefficient; but only impacts Editor and doesn't impact perf (for now)
        myGameDefns = new GameDefnsMgr();
        myGameDefns.RefreshDefns();

        // render map
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        //   GUILayout.FlexibleSpace(); // Center the box horizontally
        var width = mapDefn.Width * tileSize;
        var height = mapDefn.Height * tileSize;
        Rect gridRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        var borderRect = gridRect.Expand(2, 2);
        GUI.Box(borderRect, "");
        renderMap(gridRect, mapDefn);
        //     GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // handle drag
        if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
            HandleMouseInput(mapDefn, gridRect);

        // Show dragsquare
        if (isDragging)
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

            Repaint();
        }
    }
    Vector2 dragOffset;

    private void renderMap(Rect rect, TownDefn mapDefn)
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
            if (!buildingDefn.IsEnabled) continue;
            var finalX = rect.x + buildingDefn.TileX * tileSize;
            var finalY = rect.y + ((mapDefn.Height - 1) * tileSize - buildingDefn.TileY * tileSize);
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

    private void HandleMouseInput(TownDefn mapDefn, Rect gridRect)
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

}