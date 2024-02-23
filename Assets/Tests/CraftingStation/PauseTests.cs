using NUnit.Framework;

public partial class CraftingStationTests : MovePauseDestroyTestBase
{
    [Test]
    public void CraftingStation_PauseTests()
    {
        // For this test we craft an item that requires two wood and generates 1 gardenplot.
        // only testing explicit goods here.

        // example scenario: 2 resources needed for crafting. subtasks:
        // --- transport resouce 1 to crafting spot
        // 0    walk to resource in itemspot 1
        // 1    pickup resource from itemspot 1
        // 2    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 3    walk to crafting spot
        // 4    drop resource in crafting spot
        // --- transport resouce 2 to crafting spot
        // 5    walk to resource in itemspot 2
        // 6    pickup resource from itemspot 2
        // 7    unreserve resource spot 1 IFF it's not the spot we're dropping the crafted item into; otherwise do a noop
        // 8    walk to crafting spot
        // 9    drop resource in crafting spot
        // --- ready to craft
        // 10   craft item
        // 11   walk to storage spot
        // 12   drop item in storage spot
        for (int subtask = 0; subtask < 13; subtask++)
        {
            if (subtask == 2 || subtask == 7) continue;

            // Test A: Move craftingstation while worker1 is crafting item
            LoadTestTown("craftingstation_MovePauseDestroy", subtask); runPauseTest("Test A", subtask, false);
            LoadTestTown("craftingstation_MovePauseDestroy", subtask); runPauseTest("Test B", subtask, true);
        }
    }

    void runPauseTest(string testName, int workerSubtask, bool forceFillAllStorage)
    {
        BuildingData buildingWorker = CraftingStation;
        var buildingToPause = CraftingStation;

        TestName = $"{testName}-{workerSubtask} {(forceFillAllStorage ? "fillall" : "")}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to 1st storage spot to pick up 1st resource and bring to craftingspot"; break;
            case 1: TestName += $"picking up 1st resource"; break;
            case 2: TestName += $"Unreserving 1st resource storagespot; shouldn't hit this"; break;
            case 3: TestName += $"Carrying 1st resource to craftingspot"; break;
            case 4: TestName += $"Dropping 1st resource in craftingspot"; break;
            case 5: TestName += $"walking to 2nd storage spot to pick up 2nd resource and bring to craftingspot"; break;
            case 6: TestName += $"picking up 2nd resource"; break;
            case 7: TestName += $"Unreserving 2nd resource storagespot; shouldn't hit this"; break;
            case 8: TestName += $"Carrying 2nd resource to craftingspot"; break;
            case 9: TestName += $"Dropping 2nd resource in craftingspot"; break;
            case 10: TestName += $"Crafting the item"; break;
            case 11: TestName += $"walking to storage spot to storage crafted item"; break;
            case 12: TestName += $"dropping crafted item in storage spot"; break;
        }
        if (forceFillAllStorage) TestName += " (forceFillAllStorage)";
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = createWorkerInBuilding(buildingWorker);

        waitUntilTaskAndSubtaskIndex(worker, TaskType.Task_CraftItem, 0);

        if (forceFillAllStorage)
            fillAllTownStorageWithItem("plank");

        var newTask = getWorkerCurrentTaskAsType<Task_CraftItem>(worker);
        CraftingSpotData reservedCraftingSpot = buildingWorker.CraftingMgr.CraftingSpots[0];
        StorageSpotData resourceSpot1 = (StorageSpotData)newTask.ReservedSpots[0];
        StorageSpotData resourceSpot2 = (StorageSpotData)newTask.ReservedSpots[1];
        var origResource1 = resourceSpot1.ItemContainer.Item;
        var origResource2 = resourceSpot2.ItemContainer.Item;

        if (workerSubtask > 0)
            waitUntilTaskAndSubtaskIndex(worker, TaskType.Task_CraftItem, workerSubtask);

        buildingToPause.TogglePaused();

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        verify_spotIsUnreserved(resourceSpot1);
        verify_spotIsUnreserved(resourceSpot2);
        verify_spotIsUnreserved(reservedCraftingSpot);

        // Verify new state.
        if (workerSubtask == 0 || workerSubtask == 1 || workerSubtask == 5 || workerSubtask == 6)// Walking to 1st or 2nd storage spot to pick up resource, or picking it up
        {
            var isFirstResource = workerSubtask == 0 || workerSubtask == 1;
            verify_ItemDefnInHand(worker, null);
            verify_WorkerTaskType(TaskType.Idle, worker);
            verify_ItemInStorageSpot(resourceSpot1, isFirstResource ? origResource1 : null);
            verify_ItemInStorageSpot(resourceSpot2, origResource2);
            verify_ItemCountInCraftingSpot(reservedCraftingSpot, isFirstResource ? 0 : 1);
            verify_ItemIsInCraftingSpot(reservedCraftingSpot, isFirstResource ? null : origResource1);
            verify_ItemsOnGround(0);
        }
        else if (workerSubtask == 3 || workerSubtask == 4 || workerSubtask == 8 || workerSubtask == 9) // Walking to crafting spot to drop 1st or 2nd resource, or dropping it
        {
            var isFirstResource = workerSubtask == 3 || workerSubtask == 4;

            if (forceFillAllStorage)
            {
                verify_WorkerTaskType(TaskType.Idle, worker);
                verify_ItemDefnInHand(worker, null);
                verify_ItemsOnGround(1);
            }
            else
            {
                // we should now be carrying the resource to a storage spot in the Camp since we're paused
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                var newTask2 = getWorkerCurrentTaskAsType<Task_DeliverItemInHandToStorageSpot>(worker);
                verify_ItemDefnInHand(worker, "wood");
                var newSubtask = getWorkerCurrentSubtaskAsType<Subtask_WalkToItemSpot>(worker);
                verify_spotIsReserved(newSubtask.ItemSpot);
                verify_BuildingsAreEqual(newTask2.SpotToStoreItemIn.Building, Camp);
                verify_ItemInStorageSpot(resourceSpot1, null);
                verify_ItemInStorageSpot(resourceSpot2, isFirstResource ? origResource2 : null);
                verify_ItemCountInCraftingSpot(reservedCraftingSpot, isFirstResource ? 0 : 1);
                verify_ItemIsInCraftingSpot(reservedCraftingSpot, isFirstResource ? null : origResource1);
            }
        }
        else if (workerSubtask == 10) // Crafting the item
        {
            verify_ItemDefnInHand(worker, null);
            verify_WorkerTaskType(TaskType.Idle, worker);

            // still two items in the crafting spot
            // TODO: Should create a cleanup task to move them back to storagespots
            verify_ItemCountInCraftingSpot(reservedCraftingSpot, 2);
            verify_ItemInStorageSpot(resourceSpot1, null);
            verify_ItemInStorageSpot(resourceSpot2, null);
        }
        else if (workerSubtask == 11 || workerSubtask == 12) // walking to final storage spot to drop crafted resource, or dropping it
        {
            // we should now be carrying the crafted good to a storage spot in the Camp since we're paused
            if (forceFillAllStorage)
            {
                verify_WorkerTaskType(TaskType.Idle, worker);
                verify_ItemDefnInHand(worker, null);
                verify_ItemsOnGround(1);
            }
            else
            {
                // we should now be carrying the crafted good to a storage spot in the Camp since we're paused
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                var newTask3 = getWorkerCurrentTaskAsType<Task_DeliverItemInHandToStorageSpot>(worker);
                verify_ItemDefnInHand(worker, "GardenPlot");
                var newSubtask = getWorkerCurrentSubtaskAsType<Subtask_WalkToItemSpot>(worker);
                verify_spotIsReserved(newSubtask.ItemSpot);
                verify_BuildingsAreEqual(newTask3.SpotToStoreItemIn.Building, Camp);
                verify_ItemCountInCraftingSpot(reservedCraftingSpot, 0);
                verify_ItemInStorageSpot(resourceSpot1, null);
                verify_ItemInStorageSpot(resourceSpot2, null);
            }
        }
    }
}