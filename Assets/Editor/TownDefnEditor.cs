using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TownDefn))]
public class TownDefnEditor : OdinEditor
{
    GameDefnsMgr myGameDefns;

    public override void OnInspectorGUI()
    {
        TownDefn mapDefn = (TownDefn)Selection.activeObject;

        DrawDefaultInspector();

        // TODO: Horribly inefficient; but only impacts Editor and doesn't impact perf (for now)
        myGameDefns = new GameDefnsMgr();
        myGameDefns.RefreshDefns();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        //   GUILayout.FlexibleSpace(); // Center the box horizontally

        var width = mapDefn.Width * 64;
        var height = mapDefn.Height * 64;
        Rect gridRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        var borderRect = gridRect.Expand(2, 2);
        GUI.Box(borderRect, "");
        renderMap(gridRect, mapDefn);

        //     GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        HandleMouseInput(gridRect);
    }

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

    private void HandleMouseInput(Rect gridRect)
    {
        // Get the current event
        Event currentEvent = Event.current;

        // Check if the event is a mouse click within the gridRect
        if (currentEvent.type == EventType.MouseDown && gridRect.Contains(currentEvent.mousePosition))
        {
            // Calculate the position relative to the upper-left corner of the grid
            Vector2 relativePosition = currentEvent.mousePosition - new Vector2(gridRect.x, gridRect.y);

            // Log the relative position
            Debug.Log($"Grid Clicked at: {relativePosition}");

            // Use this to consume the event so it doesn't propagate further
            currentEvent.Use();
        }
    }
}