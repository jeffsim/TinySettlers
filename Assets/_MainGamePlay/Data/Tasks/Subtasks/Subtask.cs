using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Subtask
{
    [SerializeField] protected Task Task;
    [SerializeField] public virtual bool IsWalkingToTarget { get; set; } = false;
    [SerializeField] protected virtual float RunTime { get; set; } = 0;
    [SerializeField] public virtual bool AutomaticallyAbandonIfAssignedBuildingPaused { get; set; } = true;
    [SerializeField] public virtual bool AutomaticallyAbandonIfAssignedBuildingDestroyed { get; set; } = true;
    [SerializeField] public virtual bool AutomaticallyAbandonIfAssignedBuildingMoved { get; set; } = false;
    [SerializeField] public virtual bool InstantlyRun { get; set; } = false;

    public List<BuildingData> UpdateWorkerLocWhenTheseBuildingsMove = new();
    public List<BuildingData> UpdateMoveTargetWhenTheseBuildingsMove = new();

    // not all use this but most do
    [SerializeField] public IItemSpotInBuilding ItemSpot;

    public float StartTime;

    public float PercentDone => Math.Clamp((GameTime.time - StartTime) / RunTime, 0, 1);
    public bool IsSubstateDone => PercentDone == 1;

    public virtual ItemDefn GetTaskItem() => null;

    public Subtask(Task parentTask)
    {
        Task = parentTask;
    }

    public virtual void OnAnyBuildingMoved(BuildingData movedBuilding, LocationComponent previousLoc)
    {
    }

    public virtual void OnAnyBuildingDestroyed(BuildingData destroyedBuilding)
    {

    }

    public virtual void OnAnyBuildingPauseToggled(BuildingData building)
    {

    }

    protected void UpdateWorkerLocWhenBuildingMoves(BuildingData building)
    {
        Debug.Assert(!UpdateWorkerLocWhenTheseBuildingsMove.Contains(building));
        UpdateWorkerLocWhenTheseBuildingsMove.Add(building);
    }

    protected void UpdateMoveTargetWhenBuildingMoves(BuildingData building)
    {
        Debug.Assert(!UpdateMoveTargetWhenTheseBuildingsMove.Contains(building));
        UpdateMoveTargetWhenTheseBuildingsMove.Add(building);
    }

    public virtual void Start()
    {
        StartTime = GameTime.time;
    }

    public virtual void Update()
    {
        if (IsSubstateDone)
            Task.GotoNextSubstate();
    }

    public virtual void SubtaskComplete()
    {
    }
}