using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public abstract class TestBase
{
    public TownData Town;

    // first instance of buildings found in town
    public BuildingData Camp;
    public BuildingData MinersHut;
    public BuildingData StoneMine;
    public BuildingData Market;
    public BuildingData CraftingStation;
    public BuildingData StorageRoom;
    public BuildingData WoodcuttersHut;
    public BuildingData Forest;

    TownDefn townDefn;

    public void LoadTestTown(string townDefnName, int stepNum = 0)
    {
        GameTime.IsTest = true;
        CurStep = stepNum;
        if (GameDefns.Instance == null)
        {
            // TODO: I suspect this is slowing down the tests.  How can I avoid the need to do it, keeping in mind that the main code uses it everywhere
            var go = new GameObject("GameDefns");
            GameDefns.Instance = go.AddComponent<GameDefns>();
            GameDefns.Instance.Test_ForceAwake();
        }
        UniqueIdGenerator.Instance = new UniqueIdGenerator();

        townDefn = GameDefns.Instance.TownDefns[townDefnName];
        Town = new TownData(townDefn);
        Town.InitializeOnFirstEnter();

        Camp = getBuilding("testCamp", true);
        MinersHut = getBuilding("testMinersHut", true);
        Market = getBuilding("testMarket", true);
        CraftingStation = getBuilding("testCraftingStation", true);
        StorageRoom = getBuilding("testStorageRoom", true);
        WoodcuttersHut = getBuilding("testWoodCuttersHut", true);
        Forest = getBuilding("testForest", true);

        StoneMine = getBuilding("testStoneMine_oneGatherSpot", true);
        StoneMine ??= getBuilding("testStoneMine_twoGatherSpots", true);

        GameTime.timeScale = 16;
    }

    protected BuildingData getBuildingByTestId(string testId, bool failureIsOkay = false)
    {
        var building = Town.AllBuildings.Find(building => building.TestId == testId);
        if (building == null && !failureIsOkay)
            Assert.Fail($"{preface()} failed to get building with TestId '{testId}'");
        return building;
    }

    protected BuildingData getBuilding(string buildingDefnId, bool failureIsOkay = false)
    {
        foreach (var building in Town.AllBuildings)
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
        foreach (var worker in Town.TownWorkerMgr.Workers)
            if (worker.Assignment.AssignedTo.DefnId == assignedBuildingId)
                if (--num == -1)
                    return worker;
        Assert.Fail($"{preface()} failed to get worker {num} in building {assignedBuildingId}");
        return null;
    }

    protected void waitUntilTask(WorkerData worker, TaskType taskType, string message = "", float secondsBeforeExitCheck = 50, int frame = 2)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.AI.CurrentTask.Type != taskType)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"{preface(message, frame)} stuck in loop in waitUntilTask.  AI.CurrentTask = " + worker.AI.CurrentTask.Type + ", expected " + taskType);
    }

    protected void waitUntilTaskAndSubstate(WorkerData worker, TaskType taskType, int substate, string message = "", float secondsBeforeExitCheck = 500)
    {
        waitUntilTask(worker, taskType, message, secondsBeforeExitCheck);
        waitUntilTaskSubstate(worker, substate, message, secondsBeforeExitCheck);
    }

    protected void waitUntilTaskSubstate(WorkerData worker, int substate, string message = "", float secondsBeforeExitCheck = 500)
    {
        Assert.IsTrue(false, "nyi port");
        // float breakTime = GameTime.time + secondsBeforeExitCheck;
        // while (GameTime.time < breakTime && worker.AI.CurrentTask.substate != substate)
        // {
        //     updateTown();
        // }
        // Assert.IsTrue(GameTime.time < breakTime, $"{preface(message)} stuck in loop in waitUntilTaskSubstate.  substate = {worker.AI.CurrentTask.substate}, expected substate {substate}");
    }


    protected void waitUntilNewTask(WorkerData worker, TaskType newTaskType)
    {
        waitUntilTaskDone(worker);
        waitUntilTask(worker, newTaskType);
    }

    protected void waitUntilTaskDone(WorkerData worker, string message = "", float secondsBeforeExitCheck = 50)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        var startTask = worker.AI.CurrentTask;
        while (GameTime.time < breakTime && worker.AI.CurrentTask == startTask)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"s{preface(message)} stuck in loop in waitUntilTaskDone.  AI.CurrentTask = {worker.AI.CurrentTask.Type}, expected task to change");
    }

    public string TestName = "";
    int CurStep;
    protected void SetStep(int stepNum)
    {
        CurStep = stepNum;
    }

    protected string preface(string message = "", int frame = 2) => _preface(new System.Diagnostics.StackTrace(true).GetFrame(frame).GetFileLineNumber(), message);
    private string _preface(int lineNum, string message = "") => $"{TestName}StepNum {CurStep}, line {lineNum}: {(message != "" ? message + ": " : "")}";

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

    public void verify_BuildingsAreEqual(BuildingData building1, BuildingData building2, string message = "")
    {
        Assert.AreEqual(building1, building2, $"{preface(message)} Buildings not equal - {building1} vs {building2}");
    }

    public void verify_ItemsAreEqual(ItemData item1, ItemData item2, string message = "")
    {
        Assert.AreEqual(item1, item2, $"{preface(message)} Items not equal - {item1} vs {item2}");
    }

    public void verify_SpotsAreEqual(IItemSpotInBuilding spot1, IItemSpotInBuilding spot2, string message = "")
    {
        Assert.AreEqual(spot1, spot2, $"{preface(message)} Spots not equal - {spot1} vs {spot2}");
    }

    public void verify_WorkerTaskType(TaskType expectedType, WorkerData worker, string message = "", int frame = 2)
    {
        Assert.NotNull(worker.AI.CurrentTask, $"{preface(message, frame)}: Expected worker {worker} to have a task, but worker.AI.CurrentTask is null");
        Assert.AreEqual(expectedType, worker.AI.CurrentTask.Type, $"{preface(message, frame)} Expected worker {worker} to have task type {expectedType}, but worker.AI.CurrentTask.Type is {worker.AI.CurrentTask.Type}");
    }

    protected void verify_WorkerTaskSubstate(int substate, WorkerData worker)
    {
        Assert.IsTrue(false, "nyi port");
        // Assert.NotNull(worker.AI.CurrentTask, $"{preface(message)} Expected worker {worker} to have a task, but worker.AI.CurrentTask is null");
        // Assert.AreEqual(substate, worker.AI.CurrentTask.substate, $"{preface(message)} Expected worker {worker} to have substate {substate}, but worker.AI.CurrentTask.substate is {worker.AI.CurrentTask.substate}");
    }

    protected void waitUntilTaskAndSubtask(WorkerData worker, TaskType taskType, Type subtaskType, string message = "", float secondsBeforeExitCheck = 500)
    {
        waitUntilTask(worker, taskType, message, secondsBeforeExitCheck, 3);
        waitUntilSubtask(worker, subtaskType, message, secondsBeforeExitCheck, 3);
    }

    protected void waitUntilSubtask(WorkerData worker, Type subtaskType, string message = "", float secondsBeforeExitCheck = 500, int frame = 2)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.AI.CurrentTask.CurSubTask.GetType() != subtaskType)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"{preface(message, frame)} stuck in loop in waitUntilTaskSubstate.  substate = {worker.AI.CurrentTask.CurSubTask.GetType()}, expected substate {subtaskType}");
    }

    public void waitUntilSubtaskIndex(WorkerData worker, int subTaskIndex, string message = "", float secondsBeforeExitCheck = 500, int frame = 2)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && worker.AI.CurrentTask.SubtaskIndex != subTaskIndex)
        {
            updateTown();
        }
        Assert.IsTrue(GameTime.time < breakTime, $"{preface(message, frame)} stuck in loop in waitUntilTaskSubstate.  subTaskIndex = {worker.AI.CurrentTask.SubtaskIndex}, expected subTaskIndex {subTaskIndex}");
    }

    public void waitUntilTaskAndSubtaskIndex(WorkerData worker, TaskType taskType, int subTaskIndex, string message = "", float secondsBeforeExitCheck = 500, int frame = 2)
    {
        float breakTime = GameTime.time + secondsBeforeExitCheck;
        while (GameTime.time < breakTime && (worker.AI.CurrentTask.Type != taskType || worker.AI.CurrentTask.SubtaskIndex != subTaskIndex))
        {
            // Debug.Log($"{worker.AI.CurrentTask.Type} {taskType} {worker.AI.CurrentTask.SubtaskIndex} {subTaskIndex}");
            updateTown();
        }
        // if (GameTime.time >= breakTime)
        //     updateTown();

        Assert.IsTrue(GameTime.time < breakTime, $"{preface(message, frame)} stuck in loop in waitUntilTaskSubstate. task = {worker.AI.CurrentTask.Type} expectedTask = {taskType} subTaskIndex = {worker.AI.CurrentTask.SubtaskIndex}, expected subTaskIndex {subTaskIndex}");
    }

    public void verify_WorkerTaskTypeAndSubtask(WorkerData worker, TaskType expectedType, Type subtaskType, string message = "")
    {
        verify_WorkerTaskType(expectedType, worker, message, 3);
        verify_WorkerTaskSubtask(subtaskType, worker, message, 3);
    }

    protected void verify_WorkerTaskSubtask(Type type, WorkerData worker, string message = "", int frame = 2)
    {
        Assert.NotNull(worker.AI.CurrentTask, $"{preface(message)} Expected worker {worker} to have a task, but worker.AI.CurrentTask is null");
        Assert.AreEqual(type, worker.AI.CurrentTask.CurSubTask.GetType(), $"{preface(message, frame)} Expected worker {worker} to have substate {type}, but worker.AI.CurrentTask.substate is {worker.AI.CurrentTask.CurSubTask.GetType()}");
    }

    protected void verify_AssignedBuilding(WorkerData worker, BuildingData building, string message = "")
    {
        Assert.NotNull(worker, $"{preface(message)} Expected worker {worker} to be assigned to {building}, but worker is null");
        Assert.NotNull(building, $"{preface(message)} Expected worker {worker} to be assigned to {building}, but building is null");
        Assert.AreEqual(worker.Assignment.AssignedTo, building, $"{preface(message)} Expected worker {worker} to be assigned to {building}, but worker is assigned to '{worker.Assignment.AssignedTo}'");
    }

    protected void verify_ItemDefnInHand(WorkerData worker, string itemDefnId, string message = "")
    {
        Assert.NotNull(worker);
        if (worker.Hands.HasItem)
            Assert.AreEqual(itemDefnId, worker.Hands.Item.DefnId, $"{preface(message)} Expected item in hand to be '{itemDefnId}', but is '{worker.Hands.Item}'");
        else
            Assert.AreEqual(itemDefnId, null, $"{preface(message)} Expected item in hand to be null, but is '{itemDefnId}'");
    }


    protected void verify_ItemInHand(WorkerData worker, ItemData item, string message = "")
    {
        Assert.NotNull(worker);
        if (worker.Hands.HasItem)
            Assert.AreEqual(item, worker.Hands.Item, $"{preface(message)} Expected item in hand to be '{item}', but is '{worker.Hands.Item}'");
        else
            Assert.AreEqual(item, null, $"{preface(message)} Expected item in hand to be null, but is '{item}'");
    }

    protected void verify_spotReservedByWorker(IItemSpotInBuilding spot, WorkerData worker, string message = "")
    {
        Assert.IsNotNull(spot, $"{preface(message)} null spot");
        Assert.IsNotNull(spot.Building, $"{preface(message)} null spot.Building");
        Assert.AreEqual(spot, getStorageSpotInBuildingReservedByWorker(spot.Building, worker), $"{preface(message)} Expected spot to still be reserved by worker, but it is not");
    }

    protected void verify_spotIsUnreserved(IReservationProvider spot, string message = "")
    {
        Assert.IsNotNull(spot, $"{preface(message)} null spot");
        Assert.IsNull(spot.Reservation.ReservedBy, $"{preface(message)} Expected spot to be unreserved, but it is reserved by {spot.Reservation.ReservedBy}");
    }

    protected void verify_spotIsReserved(IItemSpotInBuilding spot, string message = "")
    {
        Assert.IsNotNull(spot, $"{preface(message)} null spot");
        Assert.IsNotNull(spot.Reservation.ReservedBy, $"{preface(message)} Expected spot to be reserved, but it is not");
    }

    protected void verify_ItemsOnGround(int expectedNumber, string message = "")
    {
        string itemsFound = string.Join(", ", Town.ItemsOnGround.Select(item => item.DefnId));
        Assert.AreEqual(expectedNumber, Town.ItemsOnGround.Count, $"{preface(message)} Expected {expectedNumber} items on ground, but found {Town.ItemsOnGround.Count} ({itemsFound})");
    }

    protected void verify_ItemInStorageSpot(StorageSpotData spot, ItemData expectedItem, string message = "")
    {
        var actualItem = spot.ItemContainer.Item;
        Assert.AreEqual(expectedItem, actualItem, $"{preface(message)} Expected item in storage spot to be '{expectedItem}', but is '{actualItem}'");
    }

    protected void verify_ItemIsInCraftingSpot(CraftingSpotData spot, ItemData expectedItem, string message = "")
    {
        var items = spot.ItemsContainer.Items;
        if (expectedItem == null)
            Assert.AreEqual(0, items.Count, $"{preface(message)} Expected no items in crafting spot, but found {items.Count} items");
        else
            Assert.IsTrue(items.Contains(expectedItem), $"{preface(message)} Expected item in crafting spot to be '{expectedItem}', but is not in the list of items in the crafting spot");
    }

    protected void verify_ItemCountInCraftingSpot(IMultipleItemSpotInBuilding spot, int expectedCount, string message = "")
    {
        var actualCount = spot.ItemsContainer.Items.Count;
        Assert.AreEqual(expectedCount, actualCount, $"{preface(message)} Expected {expectedCount} item(s) in crafting spot [{spot}], but it actually has {actualCount}");
    }

    protected void verify_ItemTypeInSpot(StorageSpotData spot, ItemDefn expectedItemType, string message = "")
    {
        Assert.NotNull(spot, $"{preface(message)} spot is null");
        var actualItem = spot.ItemContainer.Item;
        Assert.NotNull(actualItem, $"{preface(message)} actualItem is null");
        Assert.AreEqual(expectedItemType, actualItem.Defn, $"{preface(message)} Expected item in storage spot to be '{expectedItemType}', but is '{actualItem.Defn}'");
    }

    protected void verify_ItemInSpot(IItemSpotInBuilding spot, ItemData expectedItem, string message = "")
    {
        Assert.NotNull(spot, $"{preface(message)} spot is null");
        Assert.NotNull(spot.ItemContainer, $"{preface(message)} spot.ItemContainer is null");
        var actualItem = spot.ItemContainer.Item;
        if (expectedItem == null)
            Assert.AreEqual(expectedItem, actualItem, $"{preface(message)} Expected spot to be empty, but it contains '{actualItem}'");
        else
            Assert.AreEqual(expectedItem, actualItem, $"{preface(message)} Expected item in spot to be '{expectedItem}', but is '{actualItem}'");
    }

    protected void forceMoveWorkerAwayFromAssignedBuilding(WorkerData worker)
    {
        Vector3 loc = worker.Assignment.AssignedTo.Location.WorldLoc;
        worker.Location.SetWorldLoc(loc.x + 1, loc.y, loc.z);
    }

    protected StorageSpotData getStorageSpotInBuildingReservedByWorker(BuildingData building, WorkerData worker, string message = "")
    {
        Assert.NotNull(building, $"{preface(message)} building is null");
        Assert.NotNull(worker, $"{preface(message)} worker is null");
        return building.StorageSpots.Find(spot => spot.Reservation.ReservedBy == worker);
    }

    protected StorageSpotData getStorageSpotInBuildingWithItem(BuildingData building, ItemData item)
    {
        Assert.NotNull(building, $"{preface()} building is null");
        Assert.NotNull(item, $"{preface()} item is null");
        return building.StorageSpots.Find(spot => spot.ItemContainer.Item == item);
    }

    protected GatheringSpotData getGatheringSpotInBuildingWithItem(BuildingData building, ItemData item)
    {
        Assert.NotNull(building, $"{preface()} building is null");
        Assert.NotNull(item, $"{preface()} item is null");
        return building.GatheringSpots.Find(spot => spot.ItemContainer.Item == item);
    }

    protected void fillAllTownStorageWithItem(string itemDefnId)
    {
        // Fill up all storage spots so that the worker will fail when looking for a new spot to store the item in
        foreach (var building in Town.AllBuildings)
            if (building.Defn.CanStoreItems)
                foreach (var spot in building.StorageSpots)
                    if (!spot.ItemContainer.HasItem && !spot.Reservation.IsReserved)
                        spot.ItemContainer.SetItem(new ItemData() { DefnId = itemDefnId });
    }

    protected int GetNumItemsInTownStorage() => Town.AllBuildings.Sum(building => building.StorageSpots.Count(spot => spot.ItemContainer.HasItem));
    protected int GetNumItemsInTownGatheringSpots() => Town.AllBuildings.Sum(building => building.GatheringSpots.Count(spot => spot.ItemContainer.HasItem));

    protected void moveBuilding(BuildingData buildingToMove, int tileX, int tileY, string message = "")
    {
        Assert.IsNull(Town.Tiles[tileY * Town.Defn.Width + tileX].BuildingInTile, $"{preface(message)} Tile {tileX}, {tileY} is already occupied by a building");
        Town.MoveBuilding(buildingToMove, tileX, tileY);
    }
    protected ItemData CreateItem(string itemDefnId) => new() { DefnId = itemDefnId };

    protected T getWorkerCurrentTaskAsType<T>(WorkerData worker, string message = "") where T : Task
    {
        var task = worker.AI.CurrentTask;
        var actualTaskType = task.GetType();
        var expectedTaskType = typeof(T);
        Assert.IsNotNull(task, $"{preface()} Worker should have a current task");
        Assert.AreEqual(expectedTaskType, actualTaskType, $"{preface(message)} Worker's current task should be of type {expectedTaskType} but is {actualTaskType}");
        return (T)task;
    }

    protected T getWorkerCurrentSubtaskAsType<T>(WorkerData worker) where T : Subtask
    {
        var subtask = worker.AI.CurrentTask.CurSubTask;
        var actualSubtaskType = subtask.GetType();
        var expectedSubtaskType = typeof(T);
        Assert.IsNotNull(subtask, $"{preface()} Worker should have a current subtask");
        Assert.AreEqual(expectedSubtaskType, actualSubtaskType, $"{preface()} Worker's current subtask should be of type {expectedSubtaskType} but is {actualSubtaskType}");
        return (T)subtask;
    }

    protected WorkerData createWorkerInBuilding(BuildingData buildingWorker)
    {
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        forceMoveWorkerAwayFromAssignedBuilding(worker);
        return worker;
    }

    protected void updateTown(int times = 1)
    {
        for (int i = 0; i < times; i++)
        {
            GameTime.Test_Update();
            Town.Update();
        }
    }
}
