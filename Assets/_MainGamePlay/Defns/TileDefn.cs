using UnityEngine;

[CreateAssetMenu(fileName = "TileDefn")]
public class TileDefn : BaseDefn
{
    public Material TileColor;
    public string FriendlyName;

    public bool PlayerCanBuildOn = true;
    public Color EditorColor;
}
