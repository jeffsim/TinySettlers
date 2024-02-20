using UnityEngine;

[CreateAssetMenu()]
public class GameSettingsDefn : BaseDefn
{
    public Vector3 Debug_StartingOrthoCameraPosition = new(-8.8f, 12.5f, -8f);
    public Quaternion Debug_StartingOrthoCameraRotation = Quaternion.Euler(60, 0, 0);
    public float Debug_StartingOrthoCameraZoom = 30;

    public Vector3 Debug_StartingPerspCameraPosition = new(-8.8f, 12.5f, -8f);
    public Quaternion Debug_StartingPerspCameraRotation = Quaternion.Euler(45, 45, 0);
    public float Debug_StartingPerspCameraZoom = 30;
}
