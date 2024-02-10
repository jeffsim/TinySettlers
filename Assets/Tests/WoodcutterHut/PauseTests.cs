using NUnit.Framework;
using UnityEngine;

public partial class WoodcutterHutTests : MovePauseDestroyTestBase
{
    [Test]
    public void WoodcutterHut_PauseTests()
    {
        //   subtask=0: woodcutter is walking to forest to gather wood to store in [buildingToStoreItemIn]
        //   subtask=1: woodcutter is gathering wood in forest to store in [buildingToStoreItemIn]
        //   subtask=2: woodcutter is picking up wood in forest to store in [buildingToStoreItemIn]
        //   subtask=3: woodcutter is unreserving spot in forest (shouldn't hit this since should complete instantly)
        //   subtask=4: woodcutter is walking to [buildingToStoreItemIn]
        //   subtask=5: woodcutter is dropping wood in [buildingToStoreItemIn]
        //   subtask=6: woodcutter is walking to [buildingToStoreItemIn] but there are no available storage spots
        //   subtask=7: woodcutter is dropping wood in [buildingToStoreItemIn] but there are no available storage spots
        for (int subtask = 0; subtask < 8; subtask++)
        {
            if (subtask == 3) continue;

            // Test A: Pause woodcu while worker1 is getting wood from forest to store in store1
            // Test B: Pause forest while worker1 is getting wood from forest to store in store1
            // Test C: Pause store1 while worker1 is getting wood from forest to store in store1
            BuildingData store1, store2;
            PrepMPDTest("woodcutter_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runPauseTest("Test A", subtask, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test B", subtask, Forest, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test C", subtask, store1, store1);

            // Following tests disable store1 and store2 before running so that woodcutter can only store in woodcutter
            // Test E: Pause woodcutter while worker1 is getting wood from forest to store in woodcutter
            // Test F: Pause forest     while worker1 is getting wood from forest to store in woodcutter
            SetupMPDTest(out store1, out store2, true); runPauseTest("Test E", subtask, WoodcuttersHut, WoodcuttersHut);
            SetupMPDTest(out store1, out store2, true); runPauseTest("Test F", subtask, Forest, WoodcuttersHut);
        }
    }

    void runPauseTest(string testName, int workerSubtask, BuildingData buildingToPause, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Forest;
        BuildingData buildingWorker = WoodcuttersHut;

        TestName = $"{testName}-{workerSubtask}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
        switch (workerSubtask)
        {
            case 0: TestName += $"walking to {buildingWithItem.TestId} to gather wood and bring to {buildingToStoreItemIn.TestId}"; break;
            case 1: TestName += $"gathering wood in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 2: TestName += $"picking up wood in {buildingWithItem.TestId} to bring to {buildingToStoreItemIn.TestId}"; break;
            case 3: TestName += $"SHOULDN'T HIT THIS; be sure I have 'if subtask==3 continue' above.  Unreserving gathering spot."; break;
            case 4: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId}"; break;
            case 5: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId}"; break;
            case 6: TestName += $"walking to {buildingToStoreItemIn.TestId} to dropoff item picked up from {buildingWithItem.TestId} and there are no available storage spots"; break;
            case 7: TestName += $"dropping item in {buildingToStoreItemIn.TestId} after picking it up from {buildingWithItem.TestId} and there are no available storage spots"; break;
        }
        TestName += "\n  ";
        // if (workerSubtask == 0) Debug.Log(TestName);

        // Grow trees so that woodcutter can gather wood
        Forest.GatheringSpots[0].ItemContainer.SetItem(new ItemData() { DefnId = "wood" });

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        var spotWithWood = buildingWithItem.GetClosestUnreservedGatheringSpotWithItemToReap(worker.Location);
        var itemToBePickedUp = spotWithWood.ItemContainer.Item;
        var originalSpotWithItem = getGatheringSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var pausedBuildingWithItemInIt = buildingWithItem == buildingToPause;
        var pausedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToPause;
        var pausedBuildingOfWorker = buildingWorker == buildingToPause;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        waitUntilTaskAndSubtaskIndex(worker, TaskType.GatherResource, workerSubtask > 5 ? workerSubtask - 2 : workerSubtask);

        var originalTask = worker.AI.CurrentTask as Task_GatherResource;
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 5)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

        buildingToPause.TogglePaused();

        // If the worker is returning with the item in hand or task was abandoned, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2 || originalTask.IsAbandoned)
            updateTown();

        // Verify new state.
        if (workerSubtask < 3)// WorkerSubtask_WalkToItemSpot, gather, and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            verify_ItemInSpot(originalSpotWithItem, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
            if (pausedBuildingOfWorker || pausedBuildingWithItemInIt)
            {
                verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.Idle, worker);
            }
            else if (pausedBuildingItemWillBeStoredIn)
            {
                verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.GatherResource, worker);
                var newTask = worker.AI.CurrentTask as Task_GatherResource;
                Assert.AreNotEqual(newTask.SpotToStoreItemIn.Building, buildingToPause, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
            }
        }
        else if (workerSubtask < 6) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            verify_AssignedBuilding(worker, WoodcuttersHut, $"{preface("", 1)} Worker should still be assigned to the woodcutters hut");

            if (buildingToPause == buildingWithItem)
            {
                // nothing should have changed; we're already past the forest
                verify_WorkerTaskType(TaskType.GatherResource, worker, $"{preface("", 1)} Nothing should have changed");
                verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)} Nothing should have changed");
            }
            else if (pausedBuildingOfWorker)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, $"{preface("", 1)} Worker's assigned building was paused, so they should have abandoned the gather task and instead now be transporting the item to the storage spot");
                Assert.AreNotEqual((worker.AI.CurrentTask as Task_DeliverItemInHandToStorageSpot).ReservedItemSpot.Building, buildingToPause, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
                if (pausedBuildingItemWillBeStoredIn)
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, $"{preface("", 1)} Storage spot that item was going to be stored in should not be reserved");
                else
                    verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)} Storage spot that item was going to be stored in should still be reserved for it");
            }
            else // paused storage building
            {
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                if (pausedBuildingOfWorker)
                {
                    verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding");
                    Assert.AreNotEqual((worker.AI.CurrentTask as Task_DeliverItemInHandToStorageSpot).ReservedItemSpot.Building, buildingToPause, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
                }
                else
                {
                    verify_WorkerTaskType(TaskType.GatherResource, worker, "Should still be delivering the item that the worker is holding");
                    Assert.AreNotEqual((worker.AI.CurrentTask as Task_GatherResource).SpotToStoreItemIn.Building, buildingToPause, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
                }
            }
        }
        else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

            verify_AssignedBuilding(worker, workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            if (buildingToPause == buildingWithItem)
            {
                // nothing should have changed; we're already past the forest
                verify_WorkerTaskType(TaskType.GatherResource, worker, $"{preface("", 1)} Nothing should have changed");
                verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)} Nothing should have changed");
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