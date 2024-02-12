using UnityEngine;

public class CameraDragger : MonoBehaviour
{
    private Vector3 dragStartPos;
    private bool isDragging = false;
    public float zoomSensitivity = 25.0f;

    void Start()
    {
        // This uses the GameSettingsDefn to set the camera position
        transform.position = GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingCameraPosition;
        Camera.main.orthographicSize = GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingCameraZoom;
        // The value is set using the CameraPannerEditor class and stored in the GameSettingsDefn ScriptableObject in the Resources/Defns folder
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(1))
            isDragging = false;
        else if (isDragging && Input.GetMouseButton(1))
            Camera.main.transform.position -= Camera.main.ScreenToWorldPoint(Input.mousePosition) - dragStartPos;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * zoomSensitivity, 5, 50);
    }
}
