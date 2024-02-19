using UnityEngine;
using UnityEngine.EventSystems;

public class TileBackground : MonoBehaviour
{
    Tile tile;

    public void Awake()
    {
        tile = transform.GetComponentInParent<Tile>();
    }

    public void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            tile.OnClicked();
        }
    }
}