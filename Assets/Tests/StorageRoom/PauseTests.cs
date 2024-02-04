using NUnit.Framework;
using UnityEngine;

public partial class StorageRoomTests : TestBase
{
    [Test]
    public void StorageRoom_PauseTests()
    {
        //   subtask=0: Pause [buildingToPause] while [workerToTest] is walking to [buildingWithItem] to pick something up to store in [buildingToStoreItemIn]
        //   subtask=1: Pause [buildingToPause] while [workerToTest] is picking up item in [buildingWithItem] to store in [buildingToStoreItemIn]
        //   subtask=2: Pause [buildingToPause] while [workerToTest] is walking to [buildingToStoreItemIn]
        //   subtask=3: Pause [buildingToPause] while [workerToTest] is dropping item in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 4; subtask++)
        {
            // Test A: Pause store1     while worker1 is getting an item from woodcutter to store in store1
            // Test B: Pause store1     while worker2 is getting an item from woodcutter to store in store1
            // Test C: Pause store2     while worker2 is getting an item from woodcutter to store in store1
            // Test D: Pause woodcutter while worker1 is getting an item from woodcutter to store in store1
            BuildingData store1, store2;
            SetupPauseTest(subtask, out store1, out store2); runPauseTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
            SetupPauseTest(subtask, out store1, out store2); runPauseTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
            SetupPauseTest(subtask, out store1, out store2); runPauseTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
            SetupPauseTest(subtask, out store1, out store2); runPauseTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
        }
    }

    void runPauseTest(string testName, int workerSubtask, BuildingData buildingToPause, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
    {
        TestName = $"{testName}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 3: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
        }
        TestName += "\n  ";
        // Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        var itemToBePickedUp = buildingWithItem.GetUnreservedItemInStorage(GameDefns.Instance.ItemDefns["plank"]);
        var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var pausedBuildingWithItemInIt = buildingWithItem == buildingToPause;
        var pausedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToPause;

        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupItemInStorageSpot, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
        }
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);

        buildingToPause.TogglePaused();

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        // Verify new state.
        switch (workerSubtask)
        {
            case 0: // WorkerSubtask_WalkToItemSpot.
                verify_ItemDefnInHand(worker, null);
                verify_ItemInStorageSpot(originalSpotWithItem, itemToBePickedUp);

                // verify reservation of spot item is in and spot item ws going to be stored in
                // * If we paused the building the worker is assigned to,               then the task should have been fully abandoned and both spots unreserved
                // * If we paused the building that the item is in,                     then the task should have been fully abandoned and both spots unreserved
                // * If we paused the building that the item was going to be stored in, then the worker should still be walking to the itemspot and an itemspot in another building should be reserved
                if (buildingToPause == worker.Assignment.AssignedTo || pausedBuildingWithItemInIt)
                {
                    verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                    verify_WorkerTaskType(TaskType.Idle, worker);
                }
                else if (pausedBuildingItemWillBeStoredIn)
                {
                    verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                    verify_WorkerTaskType(TaskType.PickupItemInStorageSpot, worker);
                    Assert.AreNotEqual(((WorkerTask_PickupItemFromStorageSpot)worker.AI.CurrentTask).ReservedSpotToStoreItemIn.Building, buildingToPause, $"{preface()} Worker should have reserved a spot in another building to store the item in");
                }
                break;

            case 1: // WorkerSubtask_PickupItemFromBuilding.
                verify_ItemDefnInHand(worker, null);
                verify_ItemInStorageSpot(originalSpotWithItem, itemToBePickedUp);
                verify_WorkerTaskType(TaskType.Idle, worker);
                break;

            case 2: // WorkerSubtask_WalkToItemSpot.
                verify_ItemInHand(worker, itemToBePickedUp);
                verify_ItemInStorageSpot(originalSpotWithItem, null);
                verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                break;

            case 3: // WorkerSubtask_DropItemInItemSpot.
                verify_ItemInHand(worker, itemToBePickedUp);
                verify_ItemInStorageSpot(originalSpotWithItem, null);
                verify_WorkerTaskTypeAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot), "Should be carrying item to Camp now");
                verify_BuildingsAreEqual(((WorkerSubtask_WalkToItemSpot)worker.AI.CurrentTask.CurSubTask).ItemSpot.Building, Camp);
                break;
        }
    }

    void SetupPauseTest(int subtask, out BuildingData store1, out BuildingData store2)
    {
        LoadTestTown("storageRoom_move1", subtask);
        store1 = getBuildingByTestId("store1");
        store2 = getBuildingByTestId("store2");
    }
}