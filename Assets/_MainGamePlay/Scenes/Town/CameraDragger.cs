using UnityEngine;

public class CameraDragger : MonoBehaviour
{
    Vector3 dragStartPos;
    bool isDragging = false;
    float zoomSensitivity = 25.0f;
    float dollyZoomSpeed = 100.0f; // Speed for dolly zoom

    void Start()
    {
        // This uses the GameSettingsDefn to set the camera position
        Camera.main.orthographic = Settings.UseOrthographicCamera;
        if (Settings.UseOrthographicCamera)
        {
            transform.SetPositionAndRotation(GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingOrthoCameraPosition, GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingOrthoCameraRotation);
            Camera.main.orthographicSize = GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingOrthoCameraZoom;
        }
        else
        {
            transform.SetPositionAndRotation(GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingPerspCameraPosition, GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingPerspCameraRotation);
            Camera.main.fieldOfView = GameDefns.Instance.GameSettingsDefns["default"].Debug_StartingPerspCameraZoom;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            dragStartPos = GetWorldPositionFromMouseClick();
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(1))
            isDragging = false;
        else if (isDragging && Input.GetMouseButton(1))
            Camera.main.transform.position -= GetWorldPositionFromMouseClick() - dragStartPos;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (Settings.UseOrthographicCamera)
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * zoomSensitivity, 5, 50);
            else
                Camera.main.transform.position += Camera.main.transform.forward * scroll * dollyZoomSpeed;
        }
    }

    Vector3 GetWorldPositionFromMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo))
            return hitInfo.point;
        else
            return Camera.main.transform.position + Camera.main.transform.forward * 10f;
    }
}
