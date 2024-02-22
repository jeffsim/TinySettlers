using UnityEngine;

[CreateAssetMenu()]
public class GameSettingsDefn : BaseDefn
{
    public bool UseOrthographicCamera = false;

    public Vector3 Debug_StartingOrthoCameraPosition = new(-8.8f, 12.5f, -8f);
    public Quaternion Debug_StartingOrthoCameraRotation = Quaternion.Euler(60, 0, 0);
    public float Debug_StartingOrthoCameraZoom = 30;

    public Vector3 Debug_StartingPerspCameraPosition = new(-8.8f, 12.5f, -8f);
    public Quaternion Debug_StartingPerspCameraRotation = Quaternion.Euler(45, 45, 0);
    public float Debug_StartingPerspCameraZoom = 30;


    public int DefaultNumMaxWorkers = 3;
    public float ConstructionTimeInSeconds = 5;
    public bool UseBinarySaveFiles = true;

    // Testing purposes: don't snap to tiles
    public bool AllowFreeBuildingPlacement = false;

    public float RealTimeToGameTimeMultiplier = .01f;

    public float TileY = -5.5f;
    public float BuildingsY = -1f;
    public float ItemSpotsY = .5f;
    public float StorageAreaY = .5f;
    public float WorkerY = -0.5f;
    public float DraggedBuildingY = 3;
    public float ItemCarryY = 1.5f;
    public float ItemDropY = -1.3f;
}
