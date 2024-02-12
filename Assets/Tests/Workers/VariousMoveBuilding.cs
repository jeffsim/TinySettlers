using NUnit.Framework;

public partial class WorkerTests : TestBase
{
    [Test]
    public void MoveTest_Bug1()
    {
        // BUG 1: 
        //  1. addworker to camp. 
        //  2. move store while worker walking to wood.  
        //  3. ==> doesn't update SpotToStoreItem

        LoadTestTown("move_bug1", 0);
        var worker = Town.CreateWorkerInBuilding(Camp);

        waitUntilTask(worker, TaskType.TransportItemFromSpotToSpot);
        var originalTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var origSpotToStoreItem = originalTask.SpotToStoreItemIn;

        moveBuilding(StorageRoom, 0, 1);

        var newTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var newSpotToStoreItem = newTask.SpotToStoreItemIn;

        Assert.AreNotEqual(origSpotToStoreItem, newSpotToStoreItem, $"{preface("", 1)} SpotToStoreItem should change");
    }

    [Test]
    public void MoveTest_Bug2_GatherTask()
    {
        // BUG 2: 
        //  1. addworker to woodcutter. 
        //  2. wait until its carrying wood to store1.
        //  3. move store1 so it's no longer the closest building. 
        //  4. wait a sec
        //  5. move store1 so that it *is* the closest building
        //  6. ==> movetarget is wrong (blue line to random place)

        LoadTestTown("move_bug2", 0);
        var worker = Town.CreateWorkerInBuilding(WoodcuttersHut);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.GatherResource, 4);
        verify_ItemDefnInHand(worker, "wood");

        var originalTask = getWorkerCurrentTaskAsType<Task_GatherResource>(worker);
        var origSpotToStoreItem = originalTask.SpotToStoreItemIn;
        var origMoveTarget = originalTask.LastMoveToTarget;
        verify_BuildingsAreEqual(StorageRoom, origSpotToStoreItem.Building);

        moveBuilding(StorageRoom, 2, 2);
        updateTown();

        var midTask = getWorkerCurrentTaskAsType<Task_GatherResource>(worker);
        var midSpotToStoreItem = midTask.SpotToStoreItemIn;
        verify_BuildingsAreEqual(WoodcuttersHut, midSpotToStoreItem.Building);

        moveBuilding(StorageRoom, 1, 1);

        var newTask = getWorkerCurrentTaskAsType<Task_GatherResource>(worker);
        var newSpotToStoreItem = newTask.SpotToStoreItemIn;
        var newMoveTarget = newTask.LastMoveToTarget;

        verify_BuildingsAreEqual(StorageRoom, newSpotToStoreItem.Building);
        verify_SpotsAreEqual(origSpotToStoreItem, newSpotToStoreItem);
        verify_LocsAreEqual(origMoveTarget, newMoveTarget);
    }
    [Test]
    public void MoveTest_Bug2_TransportTask()
    {
        // BUG 2: 
        //  1. addworker to Camp. 
        //  2. wait until its carrying wood from woodcutter to store1.
        //  3. move store1 so it's no longer the closest building. 
        //  4. wait a sec
        //  5. move store1 so that it *is* the closest building

        LoadTestTown("move_bug2_Store", 0);
        var worker = Town.CreateWorkerInBuilding(Camp);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.TransportItemFromSpotToSpot, 3);
        verify_ItemDefnInHand(worker, "wood");

        var originalTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var origSpotToStoreItem = originalTask.SpotToStoreItemIn;
        var origMoveTarget = originalTask.LastMoveToTarget;
        verify_BuildingsAreEqual(StorageRoom, origSpotToStoreItem.Building);

        moveBuilding(StorageRoom, 2, 0);
        updateTown();

        var midTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var midSpotToStoreItem = midTask.SpotToStoreItemIn;
        verify_BuildingsAreEqual(Camp, midSpotToStoreItem.Building);

        moveBuilding(StorageRoom, 1, 1);

        var newTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var newSpotToStoreItem = newTask.SpotToStoreItemIn;
        var newMoveTarget = newTask.LastMoveToTarget;

        verify_BuildingsAreEqual(StorageRoom, newSpotToStoreItem.Building);
        verify_SpotsAreEqual(origSpotToStoreItem, newSpotToStoreItem);
        verify_LocsAreEqual(origMoveTarget, newMoveTarget);
    }
}