using System;
using NUnit.Framework;
using UnityEngine;

public abstract class TestBase
{
    public TownData Town;
    public BuildingData Camp;        // first instance of building found in town
    public BuildingData MinersHut;   // first instance of building found in town
    public BuildingData StoneMine;   // first instance of building found in town

    public void LoadTestTown(string townDefnName)
    {
        GameTime.IsTest = true;
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

        Camp = getBuilding("testCamp");
        MinersHut = getBuilding("testMinersHut");
        StoneMine = getBuilding("testStoneMine_oneGatherSpot", true);
        if (StoneMine == null)
            StoneMine = getBuilding("testStoneMine_twoGatherSpots");

        GameTime.timeScale = 16;
    }

    protected BuildingData getBuilding(string buildingDefnId, bool failureIsOkay = false)
    {
        foreach (var building in Town.Buildings)
            if (building.DefnId == buildingDefnId)
                return building;
        if (!failureIsOkay)
            Assert.Fail("failed to get building " + buildingDefnId);
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
        Assert.Fail("failed to get worker " + num + " in building " + assignedBuildingId);
        return null;
    }

    protected void waitUntilTask(WorkerData worker, TaskType taskType, float secondsBeforeExitCheck = 50)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.CurrentTask.Type != taskType)
            updateTown();
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilTask");
    }

    protected void waitUntilTaskSubstate(WorkerData worker, int substate, float secondsBeforeExitCheck = 500)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.CurrentTask.substate != substate)
            updateTown();
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilTask");
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
            updateTown();
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilTaskDone");
    }

    public void verify_LocsAreEqual(Vector3 v1, Vector3 v2, string message = "", float acceptableDelta = 0.01f)
    {
        float dx = Math.Abs(v2.x - v1.x), dy = Math.Abs(v2.y - v1.y);
        if (message.Length > 0) message += ": ";
        Assert.IsTrue(dx < acceptableDelta && dy < acceptableDelta, message + "Locs not equal - " + v1 + ", " + v2);
    }

    protected void updateTown()
    {
        GameTime.Test_Update();
        Town.Update();
    }
}
