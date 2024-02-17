using System;
using TMPro;
using UnityEngine;

public class TopBar : MonoBehaviour
{
    public TextMeshProUGUI Gold;
    public TextMeshProUGUI Workers;
    public TextMeshProUGUI Time;
    public TextMeshProUGUI HomedWorkers;

    [NonSerialized] TownData Town;

    public void InitializeForTown(TownData town)
    {
        Town = town;
    }
    public void Update()
    {
        Gold.text = "Gold: " + Town.Gold.ToString();
        Time.text = $"Time: {Utilities.ConvertToTimeString(Town.TimeMgr.TimeOfDay)} ({GameTime.time:F1})";
        Workers.text = "Workers: " + Town.TownWorkerMgr.Workers.Count + "/" + Town.TownWorkerMgr.NumMaxWorkers;
        HomedWorkers.text = "Homed: " + Town.NumHomedWorkers + "/" + Town.TownWorkerMgr.Workers.Count;
    }
}