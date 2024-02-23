using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class TownWorkerMgr
{
    [SerializeReference] TownData Town;
    public List<WorkerData> Workers = new();
    public int NumMaxWorkers;
    internal List<WorkerData> GetIdleWorkers() => new(Town.TownWorkerMgr.Workers.FindAll(w => w.AI.IsIdle)); 
    internal int NumBuildingWorkers(BuildingData building) => Workers.Count(worker => worker.Assignment.AssignedTo == building);
    [NonSerialized] public Action<WorkerData> OnWorkerCreated;

    public TownWorkerMgr(TownData town)
    {
        Town = town;
        NumMaxWorkers = 0; // Camp will add some
        town.OnBuildingAdded += OnBuildingAdded;
        town.OnBuildingRemoved += OnBuildingRemoved;
    }

    public void OnLoaded()
    {
        foreach (var worker in Workers) worker.OnLoaded();
    }

    public void Update()
    {
        foreach (var worker in Workers)
            worker.Update();
    }

    // called when a building is requesting an available worker be assigned to it
    // For now, assignment is done from Camp, so just check if Camp has any workers
    internal bool WorkerIsAvailable() => NumBuildingWorkers(Town.Camp) > 0;

    private void OnBuildingRemoved(BuildingData building)
    {
        if (building.Defn.Occupiable.WorkersCanLiveHere)
            NumMaxWorkers -= building.Defn.MaxTownWorkersIncreasedWhenBuilt;
    }

    private void OnBuildingAdded(BuildingData building)
    {
        if (building.Defn.Occupiable.WorkersCanLiveHere)
            NumMaxWorkers += building.Defn.MaxTownWorkersIncreasedWhenBuilt;
    }

    public void DestroyWorker(WorkerData worker)
    {
        Workers.Remove(worker);
        worker.OnDestroyed();
    }
    internal void AddWorker(WorkerData worker)
    {
        Workers.Add(worker);
        OnWorkerCreated?.Invoke(worker);
    }
}