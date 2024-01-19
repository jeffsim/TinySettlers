using TreeEditor;
using UnityEngine;

public class CameraDragger : MonoBehaviour
{
    private Vector3 lastMousePosition;
    private bool isDragging = false;

    // You can adjust these values to control zoom sensitivity and limits
    public float zoomSensitivity = 25.0f;
    public float minZoomDistance = 5.0f;
    public float maxZoomDistance = 50.0f;

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
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            float scaleFactor = 0.01f;
            delta *= scaleFactor;

            Vector3 movement = transform.right * delta.x + Vector3.up * delta.y;
            transform.Translate(-movement, Space.World);
        }

        // Zoom functionality
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Vector3 direction = transform.forward;
            float zoomAmount = scroll * zoomSensitivity;
            Vector3 newPosition = transform.position + direction * zoomAmount;

            Camera.main.orthographicSize -= zoomAmount;

            // Optional: Clamp the zoom to prevent the camera from going too far or too close
            float distance = Vector3.Distance(newPosition, transform.position);
            // Debug.Log(distance);
            //    if (distance >= minZoomDistance && distance <= maxZoomDistance)
            {
                transform.position = newPosition;
            }
        }
    }
}
