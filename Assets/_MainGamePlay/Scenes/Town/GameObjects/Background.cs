using UnityEngine;
using UnityEngine.EventSystems;

public class Background : MonoBehaviour
{
    public SceneWithMap scene;

    public void Initialize(SceneWithMap scene)
    {
        this.scene = scene;
    }
    Vector2 dragStart;
    public void OnMouseDown()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            dragStart = Input.mousePosition;
        }
    }
    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (scene.AnyDialogIsOpen())
                scene.HideAllDialogs();
            else
            {
                if (Vector2.Distance(dragStart, Input.mousePosition) > 10)
                    return;
                Vector3 mouseScreenPosition = Input.mousePosition;
                mouseScreenPosition.z = Camera.main.WorldToScreenPoint(transform.position).z;
                var loc = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
                // var loc = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                scene.SelectBuildingToConstruct.ShowAtWorldLoc(scene, loc);
            }
        }
    }
}
