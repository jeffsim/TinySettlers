using UnityEngine;

[CreateAssetMenu()]
public class GameSettingsDefn : BaseDefn
{
    public Vector3 Debug_StartingCameraPosition = new(-8.8f, 12.5f, -8f);
    public Quaternion Debug_StartingCameraRotation = Quaternion.Euler(90, 0, 0);
    public float Debug_StartingCameraZoom = 30;
}
