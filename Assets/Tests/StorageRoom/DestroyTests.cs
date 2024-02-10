using System;
using NUnit.Framework;

public partial class StorageRoomTests : MovePauseDestroyTestBase
{
    [Test]
    public void StorageRoom_DestroyTests()
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

            // Test A: Destroy store1 while worker1 is getting an item from woodcutter to store in store1
            // Test B: Destroy store1 while worker2 is getting an item from woodcutter to store in store1
            // Test C: Destroy store2 while worker2 is getting an item from woodcutter to store in store1
            // Test D: Destroy woodcu while worker1 is getting an item from woodcutter to store in store1
            // Test E: Destroy woodcu while worker2 is getting an item from woodcutter to store in store1
            BuildingData store1, store2;
            PrepMPDTest("storageRoom_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test A", subtask, store1, store1, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test B", subtask, store1, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test C", subtask, store2, store2, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test D", subtask, WoodcuttersHut, store1, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test E", subtask, WoodcuttersHut, store2, WoodcuttersHut, store1);
        }
    }

    void runDestroyTest(string testName, int workerSubtask, BuildingData buildingToDestroy, BuildingData buildingWorker, BuildingData buildingWithItem, BuildingData buildingToStoreItemIn)
    {
        TestName = $"{testName}-{workerSubtask}: Destroy {buildingToDestroy.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to pickup item and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"picking up item in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 3: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
            case 4: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId} and there are no available storage spots"; break;
            case 5: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId} and there are no available storage spots"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = createWorkerInBuilding(buildingWorker);
        var itemToBePickedUp = buildingWithItem.GetUnreservedItemInStorage(GameDefns.Instance.ItemDefns["plank"]);
        var originalSpotWithItem = getStorageSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var destroyedBuildingWithItemInIt = buildingWithItem == buildingToDestroy;
        var destroyedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToDestroy;
        var destroyedBuildingOfWorker = buildingWorker == buildingToDestroy;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        waitUntilTaskAndSubtaskIndex(worker, TaskType.TransportItemFromSpotToSpot, workerSubtask > 4 ? workerSubtask - 2 : workerSubtask);

        var originalTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 4)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

        Town.DestroyBuilding(buildingToDestroy);

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 3 || originalTask.IsAbandoned)
            updateTown();

        // Verify new state.
        verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);
        if (workerSubtask < 2)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            if (destroyedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.PickupItemFromGround, worker, $"{preface("", 1)} Worker should be picking up the item that was dropped from the building"); // will become Task_CleanupItemOnGround
                verify_ItemsOnGround(1);
            }
            else if (destroyedBuildingOfWorker)
            {
                // worker is now assigned to camp, and it getting the same item (maybe?)
                verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be reserved");
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker);
            }
            else if (destroyedBuildingItemWillBeStoredIn)
            {
                verify_ItemInStorageSpot(originalSpotWithItem, itemToBePickedUp);
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");

                verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be reserved");
                verify_WorkerTaskType(TaskType.TransportItemFromSpotToSpot, worker, $"{preface("", 1)} Worker should still be transporting the item, but to a new building");
                Assert.AreNotEqual(originalTask.SpotToStoreItemIn.Building, buildingToDestroy, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
            }
            else Assert.Fail($"{preface("", 1)} unhandled case");
        }
        else if (workerSubtask < 5) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_ItemInStorageSpot(originalSpotWithItem, null);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            if (destroyedBuildingOfWorker)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding even though their building was destroyed");
                var newTask = getWorkerCurrentTaskAsType<Task_DeliverItemInHandToStorageSpot>(worker);
                if (destroyedBuildingItemWillBeStoredIn)
                {
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                    Assert.AreNotEqual(newTask.ReservedItemSpot.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
                }
                else
                {
                    // could have found a closer spot
                    var newSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
                    if (originalSpotToStoreItemIn == newSpotToStoreItemIn)
                        verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
                    else
                        verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)}");
                }
            }
            else if (destroyedBuildingWithItemInIt)
            {
                var newTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
                Assert.AreEqual(originalTask, newTask, $"{preface("", 1)} Worker should have same task");
                verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be reserved");
            }
            else if (destroyedBuildingItemWillBeStoredIn)
            {
                var newTask = getWorkerCurrentTaskAsType<Task_TransportItemFromSpotToSpot>(worker);
                Assert.AreEqual(originalTask, newTask, $"{preface("", 1)} Worker should have same task");

                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                Assert.AreNotEqual(newTask.SpotToStoreItemIn.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
            }
            else Assert.Fail($"{preface("", 1)} unhandled case");
        }
        else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

            verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            if (destroyedBuildingWithItemInIt)
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
                if (destroyedBuildingItemWillBeStoredIn)
                {
                    verify_ItemInHand(worker, null);
                    verify_WorkerTaskType(TaskType.Idle, worker);
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                }
                else
                {
                    verify_ItemInHand(worker, itemToBePickedUp);
                    verify_spotReservedByWorker(originalSpotToStoreItemIn, worker);
                    if (destroyedBuildingOfWorker)
                        verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding");
                    else
                        verify_WorkerTaskType(TaskType.GatherResource, worker, "Should still be delivering the item that the worker is holding");
                }
            }
        }
    }
}