using System;
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
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilTask.  CurrentTask = " + worker.CurrentTask.Type + ", expected " + taskType);
    }

    protected void waitUntilTaskAndSubstate(WorkerData worker, TaskType taskType, int substate, float secondsBeforeExitCheck = 500)
    {
        waitUntilTask(worker, taskType, secondsBeforeExitCheck);
        waitUntilTaskSubstate(worker, substate, secondsBeforeExitCheck);
    }

    protected void waitUntilTaskSubstate(WorkerData worker, int substate, float secondsBeforeExitCheck = 500)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.CurrentTask.substate != substate)
            updateTown();
        Assert.IsTrue(GameTime.time < breakTime, "stuck in loop in waitUntilTaskSubstate.  CurrentSubstate = " + worker.CurrentTask.substate + ", expected " + substate);
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

    public void verify_WorkerTaskType(TaskType expectedType, WorkerData worker)
    {
        Assert.NotNull(worker.CurrentTask);
        Assert.AreEqual(expectedType, worker.CurrentTask.Type);
    }

    protected void verify_WorkerTaskSubstate(int substate, WorkerData miner)
    {
        Assert.NotNull(miner.CurrentTask);
        Assert.AreEqual(substate, miner.CurrentTask.substate);
    }

    protected void verify_AssignedBuilding(WorkerData worker, BuildingData building)
    {
        Assert.NotNull(worker);
        Assert.NotNull(building);
        Assert.AreEqual(worker.AssignedBuilding, building);
    }

    protected void verify_ItemInHand(WorkerData worker, string itemDefnId)
    {
        Assert.NotNull(worker);
        if (worker.ItemInHand == null)
            Assert.AreEqual(itemDefnId, null);
        else
            Assert.AreEqual(itemDefnId, worker.ItemInHand.DefnId);
    }

    protected void updateTown()
    {
        GameTime.Test_Update();
        Town.Update();
    }
}
