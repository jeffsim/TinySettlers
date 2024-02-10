using NUnit.Framework;

public partial class StorageRoomTests : MovePauseDestroyTestBase
{
    [Test]
    public void StorageRoom_PauseTests()
    {
        // subtask=0: Pause [buildingToPause] while [workerToTest] is walking to [buildingWithItem] to pick something up to store in [buildingToStoreItemIn]
        // subtask=1: Pause [buildingToPause] while [workerToTest] is picking up item in [buildingWithItem] to store in [buildingToStoreItemIn]
        // subtask=2: worker is unreserving spot item was in (shouldn't hit this since should complete instantly)
        // subtask=3: Pause [buildingToPause] while [workerToTest] is walking to [buildingToStoreItemIn]
        // subtask=4: Pause [buildingToPause] while [workerToTest] is dropping item in [buildingToStoreItemIn]
        // subtask=5: Destroy [buildingToDestroy] while [workerToTest] is walking to [buildingToStoreItemIn] and there are no available storage spots
        // subtask=6: Destroy [buildingToDestroy] while [workerToTest] is dropping item in [buildingToStoreItemIn] and there are no available storage spots
        for (int subtask = 0; subtask < 6; subtask++)
        {
            if (subtask == 2) continue;

            // Test A: Pause store1 while worker1 is getting an item from woodcutter to store in store1
            // Test B: Pause store1 while worker2 is getting an item from woodcutter to store in store1
            // Test C: Pause store2 while worker2 is getting an item from woodcutter to store in store1
            // Test D: Pause woodcu while worker1 is getting an item from woodcutter to store in store1
            // Test E: Pause woodcu while worker2 is getting an item from woodcutter to store in store1
            BuildingData store1, store2;
            PrepMPDTest("storageRoom_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runPauseTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test E", subtask, WoodcuttersHut, store2, WoodcuttersHut, store1);
        }
    }

    void runPauseTest(string testName, int workerSubtask, BuildingData buildingToPause, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
    {
        TestName = $"{testName}-{workerSubtask}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"SHOULDN'T HIT THIS; be sure I have 'if subtask==3 continue' above.  Unreserving gathering spot."; break;
            case 3: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 4: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
            case 5: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId} and there are no available storage spots"; break;
            case 6: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId} and there are no available storage spots"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = createWorkerInBuilding(buildingWorker);
        var itemToBePickedUp = buildingWithItem.GetUnreservedItemInStorage(GameDefns.Instance.ItemDefns["plank"]);
        var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var pausedBuildingWithItemInIt = buildingWithItem == buildingToPause;
        var pausedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToPause;
        var pausedBuildingOfWorker = buildingWorker == buildingToPause;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        waitUntilTaskAndSubtaskIndex(worker, TaskType.TransportItemFromSpotToSpot, workerSubtask > 4 ? workerSubtask - 2 : workerSubtask);

        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface()} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 4)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

        buildingToPause.TogglePaused();

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 3)
            updateTown();

        // Verify new state.
        if (workerSubtask < 2)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            verify_ItemInStorageSpot(originalSpotWithItem, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");

            if (pausedBuildingOfWorker || pausedBuildingWithItemInIt)
            {
                verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.Idle, worker);
            }
            else if (pausedBuildingItemWillBeStoredIn)
            {
                verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker);
                Assert.AreNotEqual(((Task_TransportItemFromSpotToSpot)worker.AI.CurrentTask).SpotToStoreItemIn.Building, buildingToPause, $"{preface()} Worker should have reserved a spot in another building to store the item in");
            }
            else Assert.Fail($"{preface("", 1)} Unhandled case");
        }
        else if (workerSubtask < 5) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            verify_AssignedBuilding(worker, buildingWorker, $"{preface("", 1)} Worker should still be assigned to the storage room");

            if (pausedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker, $"{preface("", 1)} Should still be transporting item");
                verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)} Should be transporting to different building");
            }
            else if (pausedBuildingOfWorker) // ... but not building with item in it
            {
                // e.g.: Pause store1 while store1's worker is walking to store1 to dropoff item picked up from woodc
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, $"{preface("", 1)} Should now be delivering item");
                // The worker could have found a better reserved spot. If so, then the original spot should be unreserved.
                var newSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
                if (originalSpotToStoreItemIn == newSpotToStoreItemIn)
                    verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
                else
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
            }
            else if (pausedBuildingItemWillBeStoredIn)
            {
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker, $"{preface("", 1)} Should still be transporting item");
                verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)} Should be transporting to different building");
            }
            else Assert.Fail($"{preface("", 1)} Unhandled case");
        }
        else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

            verify_AssignedBuilding(worker, workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            if (pausedBuildingWithItemInIt)
            {
                // nothing should have changed; we're already past the pick up spot
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker, $"{preface("", 1)} Nothing should have changed");
                // The worker could have found a better reserved spot. If so, then the original spot should be unreserved.
                var newSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
                if (originalSpotToStoreItemIn == newSpotToStoreItemIn)
                    verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
                else
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
            }
            else
            {
                // if we paused the building to store in then worker can't deliver, so drops item to ground. in all other cases is still carrying it, but if
                // assigned building was paused then they're carrying to Camp now
                if (buildingToPause == buildingToStoreItemIn)
                {
                    verify_ItemInHand(worker, null);
                    verify_WorkerTaskType(TaskType.Idle, worker);
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                }
                else
                {
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_spotReservedByWorker(originalSpotToStoreItemIn, worker);
                    if (pausedBuildingOfWorker)
                        verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding");
                    else
                        verify_WorkerTaskType(TaskType.GatherResource, worker, "Should still be delivering the item that the worker is holding");
                }
            }
        }
    }
}