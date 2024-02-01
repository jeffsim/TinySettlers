using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public abstract class TestBase
{
    public TownData Town;
    public BuildingData Camp;        // first instance of building found in town
    public BuildingData MinersHut;   // first instance of building found in town
    public BuildingData StoneMine;   // first instance of building found in town
    public BuildingData Market;   // first instance of building found in town
    public BuildingData CraftingStation;   // first instance of building found in town

    public void LoadTestTown(string townDefnName, int stepNum = -1)
    {
        GameTime.IsTest = true;
        CurStep = stepNum;
        if (GameDefns.Instance == null)
        {
            var go = new GameObject("GameDefns");
            GameDefns.Instance = go.AddComponent<GameDefns>();
            GameDefns.Instance.Test_ForceAwake();
        }
        UniqueIdGenerator.Instance = new UniqueIdGenerator();

        var townDefn = GameDefns.Instance.TownDefns[townDefnName];
        Town = new TownData(townDefn, TownState.Available);
        Town.InitializeOnFirstEnter();

        Camp = getBuilding("testCamp", true);
        MinersHut = getBuilding("testMinersHut", true);
        Market = getBuilding("testMarket", true);
        CraftingStation = getBuilding("testCraftingStation", true);
        StoneMine = getBuilding("testStoneMine_oneGatherSpot", true);
        if (StoneMine == null)
            StoneMine = getBuilding("testStoneMine_twoGatherSpots", true);

        GameTime.timeScale = 16;
    }

    // protected WorkerData addWorkerToBuilding(BuildingData building)
    // {
    //     throw new NotImplementedException();
    // }

    // protected void addItemsToBuilding(BuildingData building, string itemDefnId, int numToAdd)
    // {
    //     throw new NotImplementedException();
    // }

    // protected BuildingData addBuilding(string buildingDefnId, int tileX, int tileY)
    // {
    //     throw new NotImplementedException();
    // }

    protected BuildingData getBuilding(string buildingDefnId, bool failureIsOkay = false)
    {
        foreach (var building in Town.Buildings)
            if (building.DefnId == buildingDefnId)
                return building;
        if (!failureIsOkay)
            Assert.Fail($"{preface()} failed to get building {buildingDefnId}");
        return null;
    }
    protected WorkerData getAssignedWorker(BuildingData building, int num = 0)
    {
        return getAssignedWorker(building.DefnId, num);
    }

    protected WorkerData getAssignedWorker(string assignedBuildingId, int num = 0)
    {
        foreach (var worker in Town.Workers)
            if (worker.AssignedBuilding.DefnId == assignedBuildingId)
                if (--num == -1)
                    return worker;
        Assert.Fail($"{preface()} failed to get worker {num} in building {assignedBuildingId}");
        return null;
    }

    protected void waitUntilTask(WorkerData worker, TaskType taskType, float secondsBeforeExitCheck = 50)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.CurrentTask.Type != taskType)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, "{preface()} stuck in loop in waitUntilTask.  CurrentTask = " + worker.CurrentTask.Type + ", expected " + taskType);
    }

    protected void waitUntilTaskAndSubstate(WorkerData worker, TaskType taskType, int substate, float secondsBeforeExitCheck = 500)
    {
        waitUntilTask(worker, taskType, secondsBeforeExitCheck);
        waitUntilTaskSubstate(worker, substate, secondsBeforeExitCheck);
    }

    protected void waitUntilTaskSubstate(WorkerData worker, int substate, float secondsBeforeExitCheck = 500)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        Debug.Log($"wait2 {worker.CurrentTask.substate} {substate} {GameTime.time < breakTime} {GameTime.time} {breakTime}");
        while (GameTime.time < breakTime && worker.CurrentTask.substate != substate)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"{preface()} stuck in loop in waitUntilTaskSubstate.  substate = {worker.CurrentTask.substate}, expected substate {substate}");
    }

    protected void waitUntilNewTask(WorkerData worker, TaskType newTaskType)
    {
        waitUntilTaskDone(worker);
        waitUntilTask(worker, newTaskType);
    }

    protected void waitUntilTaskDone(WorkerData worker, float secondsBeforeExitCheck = 50)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        var startTask = worker.CurrentTask;
        while (GameTime.time < breakTime && worker.CurrentTask == startTask)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"s{preface()} stuck in loop in waitUntilTaskDone.  CurrentTask = {worker.CurrentTask.Type}, expected task to change");
    }

    int CurStep;
    protected void SetStep(int stepNum)
    {
        CurStep = stepNum;
    }

    private string preface(string message = "") => preface(new System.Diagnostics.StackTrace(true).GetFrame(2).GetFileLineNumber(), message);
    private string preface(int lineNum, string message = "") => $"StepNum {CurStep}, line {lineNum}: {(message != "" ? message + ": " : "")}";

    public void verify_LocsAreEqual(Vector3 v1, Vector3 v2, string message = "", float acceptableDelta = 0.01f)
    {
        float dx = Math.Abs(v2.x - v1.x), dy = Math.Abs(v2.y - v1.y);
        Assert.IsTrue(dx < acceptableDelta && dy < acceptableDelta, $"{preface(message)} Locs not equal - {v1} vs {v2}");
    }

    public void verify_LocsAreEqual(LocationComponent loc1, LocationComponent loc2, string message = "", float acceptableDelta = 0.01f)
    {
        float dx = Math.Abs(loc2.WorldLoc.x - loc1.WorldLoc.x), dy = Math.Abs(loc2.WorldLoc.y - loc1.WorldLoc.y);
        Assert.IsTrue(dx < acceptableDelta && dy < acceptableDelta, $"{preface(message)} Locs not equal - {loc1} vs {loc2}");
    }

    public void verify_WorkerTaskType(TaskType expectedType, WorkerData worker)
    {
        Assert.NotNull(worker.CurrentTask, $"{preface()}: Expected worker {worker} to have a task, but worker.CurrentTask is null");
        Assert.AreEqual(expectedType, worker.CurrentTask.Type, $"{preface()} Expected worker {worker} to have task type {expectedType}, but worker.CurrentTask.Type is {worker.CurrentTask.Type}");
    }

    protected void verify_WorkerTaskSubstate(int substate, WorkerData worker)
    {
        Assert.NotNull(worker.CurrentTask, $"{preface()} Expected worker {worker} to have a task, but worker.CurrentTask is null");
        Assert.AreEqual(substate, worker.CurrentTask.substate, $"{preface()} Expected worker {worker} to have substate {substate}, but worker.CurrentTask.substate is {worker.CurrentTask.substate}");
    }

    protected void verify_AssignedBuilding(WorkerData worker, BuildingData building)
    {
        Assert.NotNull(worker, $"{preface()} Expected worker {worker} to be assigned to {building}, but worker is null");
        Assert.NotNull(building, $"{preface()} Expected worker {worker} to be assigned to {building}, but building is null");
        Assert.AreEqual(worker.AssignedBuilding, building, $"{preface()} Expected worker {worker} to be assigned to {building}, but worker is assigned to '{worker.AssignedBuilding}'");
    }

    protected void verify_ItemInHand(WorkerData worker, string itemDefnId)
    {
        Assert.NotNull(worker);
        if (worker.ItemInHand == null)
            Assert.AreEqual(itemDefnId, null, $"{preface()} Expected item in hand to be null, but is '{itemDefnId}'");
        else
            Assert.AreEqual(itemDefnId, worker.ItemInHand.DefnId, $"{preface()} Expected item in hand to be '{itemDefnId}', but is '{worker.ItemInHand}'");
    }

    protected void verify_ItemsOnGround(int expectedNumber)
    {
        if (Town.ItemsOnGround.Count != expectedNumber)
        {
            string itemsFound = string.Join(", ", Town.ItemsOnGround.Select(item => item.DefnId));
            Assert.AreEqual(expectedNumber, Town.ItemsOnGround.Count, $"{preface()} Expected {expectedNumber} items on ground, but found only {Town.ItemsOnGround.Count} ({itemsFound})");
        }
    }

    protected void forceMoveWorkerAwayFromAssignedBuilding(WorkerData worker)
    {
        var loc = worker.AssignedBuilding.Location.WorldLoc + new Vector3(1, 0, 0);
        worker.Location.SetWorldLoc(loc.x, loc.y);
    }

    protected void updateTown()
    {
        GameTime.Test_Update();
        Town.Update();
    }
}
