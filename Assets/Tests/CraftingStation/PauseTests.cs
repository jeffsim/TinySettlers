using NUnit.Framework;
using UnityEngine;

public partial class CraftingStationTests : MovePauseDestroyTestBase
{
    [Test]
    public void CraftingStation_PauseTests()
    {
        // subtask=0: woodcutter is walking to forest to gather wood to store in [buildingToStoreItemIn]
        // subtask=1: woodcutter is gathering wood in forest to store in [buildingToStoreItemIn]
        // subtask=2: woodcutter is picking up wood in forest to store in [buildingToStoreItemIn]
        // subtask=3: woodcutter is walking to [buildingToStoreItemIn]
        // subtask=4: woodcutter is dropping wood in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 6; subtask++)
        {
            // Test A: Pause woodcu while worker1 is getting wood from forest to store in store1
            // Test B: Pause forest while worker1 is getting wood from forest to store in store1
            // Test C: Pause store1 while worker1 is getting wood from forest to store in store1
            BuildingData store1, store2;
            PrepMPDTest("craftingstation_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runPauseTest("Test A", subtask, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test B", subtask, Forest, store1);
            SetupMPDTest(out store1, out store2); runPauseTest("Test C", subtask, store1, store1);

            // Following tests disable store1 and store2 before running so that woodcutter can only store in woodcutter
            // Test E: Pause woodcutter while worker1 is getting wood from forest to store in woodcutter
            // Test F: Pause forest     while worker1 is getting wood from forest to store in woodcutter
            SetupMPDTest(out store1, out store2, true); runPauseTest("Test E", subtask, WoodcuttersHut, WoodcuttersHut);
            SetupMPDTest(out store1, out store2, true); runPauseTest("Test F", subtask, Forest, WoodcuttersHut);
        }
        Assert.Fail("nyi");
    }

    void runPauseTest(string testName, int workerSubtask, BuildingData buildingToPause, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Forest;
        BuildingData buildingWorker = WoodcuttersHut;

        TestName = $"{testName}-{workerSubtask}: Pause {buildingToPause.TestId} while {buildingWorker.TestId}'s worker is ";
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

        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.GetGatherableResource, typeof(Subtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.GetGatherableResource, typeof(Subtask_PickupItemFromItemSpot)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(Subtask_WalkToItemSpot)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(Subtask_DropItemInItemSpot)); break;
            case 4: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(Subtask_WalkToItemSpot)); break;
            case 5: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(Subtask_DropItemInItemSpot)); break;
        }
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface()} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 3)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

        buildingToPause.TogglePaused();

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        // Verify new state.
        if (workerSubtask == 0 || workerSubtask == 1)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
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
                verify_WorkerTaskType(TaskType.GetGatherableResource, worker);
                Assert.AreNotEqual(((Task_GatherResource)worker.AI.CurrentTask).SpotToStoreItemIn.Building, buildingToPause, $"{preface()} Worker should have reserved a spot in another building to store the item in");
            }
        }
        else if (workerSubtask == 2 || workerSubtask == 3) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            var task = worker.AI.CurrentTask as Task_DeliverItemInHandToStorageSpot;

            if (pausedBuildingOfWorker)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding even though their building was paused");
                if (pausedBuildingItemWillBeStoredIn)
                {
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                    Assert.AreNotEqual(task.ReservedItemSpot.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
                }
                else
                {
                    verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should still be reserved");
                    Assert.AreEqual(task.ReservedItemSpot.Building, originalSpotToStoreItemIn.Building, $"{preface("", 1)} Worker should still have reserved the same spot to store the item in");
                }
            }
            if (pausedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be reserved");
            }
            if (pausedBuildingItemWillBeStoredIn)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                Assert.AreNotEqual(task.ReservedItemSpot.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
            }
        }
        else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;

            verify_AssignedBuilding(worker, workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            if (buildingToPause == buildingToStoreItemIn)
            {
                verify_ItemInHand(worker, null);
                verify_WorkerTaskType(TaskType.Idle, worker);
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
            }
            else
            {
                verify_ItemInHand(worker, itemToBePickedUp);
                verify_spotStillReservedByWorker(originalSpotToStoreItemIn, originalSpotToStoreItemIn.Building, worker);
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding");
            }
        }
    }
}