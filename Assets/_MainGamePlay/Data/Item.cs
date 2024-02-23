using System;
using UnityEngine;

[Serializable]
public class ItemData : BaseData
{
    public override string ToString() => DefnId + " (" + InstanceId + ")";

    private ItemDefn _defn;
    public ItemDefn Defn => _defn = _defn != null ? _defn : GameDefns.Instance.ItemDefns[DefnId];
   
    public string DefnId;
    public Location Location = new();
}