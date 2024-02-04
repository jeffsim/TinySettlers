using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TownDefn))]
public class TownDefnEditor : OdinEditor
{
    GameDefnsMgr myGameDefns;

    public override void OnInspectorGUI()
    {
        TownDefn mapDefn = (TownDefn)Selection.activeObject as TownDefn;

        DrawDefaultInspector();

        // TODO: Horribly inefficient; but only impacts Editor and doesn't impact perf (for now)
        myGameDefns = new GameDefnsMgr();
        myGameDefns.RefreshDefns();

        EditorGUILayout.BeginHorizontal();
        Rect rect = EditorGUILayout.GetControlRect(true, 64);
        if (rect.width > 2)
            renderMap(rect, mapDefn);
        EditorGUILayout.EndHorizontal();
    }

    private void renderMap(Rect rect, TownDefn mapDefn)
    {
        int width = mapDefn.Width, height = mapDefn.Height;
        float tileSize = Mathf.Min(rect.width * 2 / width, rect.height * 2 / height);

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
            var name = buildingDefn.Building.FriendlyName;
            if (name.Length > 2)
            {
                var text = name.Substring(0, Math.Min(name.Length, 6));
                EditorGUI.LabelField(new Rect(finalX + 2, finalY + 1, tileSize - 3, tileSize - 3), text, shadowStyle);
                EditorGUI.LabelField(new Rect(finalX + 1, finalY, tileSize - 3, tileSize - 3), text, style);
            }
        }
    }
}