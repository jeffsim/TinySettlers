using System;
using UnityEngine;

[Serializable]
public class ItemData : BaseData
{
    private ItemDefn _defn;
    public ItemDefn Defn
    {
        get
        {
            if (_defn == null)
                _defn = GameDefns.Instance.ItemDefns[DefnId];
            return _defn;
        }
    }
    public string DefnId;

    public Vector3 WorldLocOnGround;
}