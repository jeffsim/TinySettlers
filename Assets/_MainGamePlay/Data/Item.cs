using System;
using UnityEngine;

[Serializable]
public class ItemData : BaseData
{
    public override string ToString() => DefnId + " (" + InstanceId + ")";

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
    public LocationComponent Location = new();
}