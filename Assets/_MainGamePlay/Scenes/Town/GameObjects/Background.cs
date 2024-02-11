using UnityEngine;
using UnityEngine.EventSystems;

public class Background : MonoBehaviour
{
    public SceneWithMap scene;

    public void Initialize(SceneWithMap scene)
    {
        this.scene = scene;
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (scene.AnyDialogIsOpen())
                scene.HideAllDialogs();
            else if (Settings.AllowFreeBuildingPlacement)
                scene.SelectBuildingToConstruct.ShowAtWorldLoc(scene, Input.mousePosition);
        }
    }
}
