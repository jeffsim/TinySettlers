using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraDragger))]
public class CameraDraggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CameraDragger script = (CameraDragger)target;

        if (GUILayout.Button("Save Camera Position"))
        {
            // This uses the GameSettingsDefn to set the camera position
            var gameSettingsDefn = GameDefns.Instance.GameSettingsDefns["default"];
            if (gameSettingsDefn == null)
            {
                Debug.LogError("GameSettingsDefn with id 'default' not found in Resources/Defns/GameSettings.  Please create one");
                return;
            }
            if (Settings.Current.UseOrthographicCamera)
            {
                gameSettingsDefn.Debug_StartingOrthoCameraPosition = script.transform.position;
                gameSettingsDefn.Debug_StartingOrthoCameraRotation = script.transform.rotation;
                gameSettingsDefn.Debug_StartingOrthoCameraZoom = Camera.main.orthographicSize;
            }
            else
            {
                gameSettingsDefn.Debug_StartingPerspCameraPosition = script.transform.position;
                gameSettingsDefn.Debug_StartingPerspCameraRotation = script.transform.rotation;
                gameSettingsDefn.Debug_StartingPerspCameraZoom = Camera.main.fieldOfView;
            }
            EditorUtility.SetDirty(gameSettingsDefn);
            Debug.Log("Camera position saved in Resources/Defns/GameSettings (default)");
        }
    }
}