using System.Linq;
using NUnit.Framework;

public partial class WoodcutterHutTests : MovePauseDestroyTestBase
{
    [Test]
    public void WoodcutterHut_DestroyTests()
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

            // Test A: Destroy woodcu while worker1 is getting wood from forest to store in store1
            // Test B: Destroy forest while worker1 is getting wood from forest to store in store1
            // Test C: Destroy store1 while worker1 is getting wood from forest to store in store1
            BuildingData store1, store2;
            PrepMPDTest("woodcutter_MovePauseDestroy", subtask);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test A", subtask, WoodcuttersHut, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test B", subtask, Forest, store1);
            SetupMPDTest(out store1, out store2); runDestroyTest("Test C", subtask, store1, store1);

            // Following tests disable store1 and store2 before running so that woodcutter can only store in woodcutter
            // Test E: Destroy woodcutter while worker1 is getting wood from forest to store in woodcutter
            // Test F: Destroy forest     while worker1 is getting wood from forest to store in woodcutter
            SetupMPDTest(out store1, out store2, true); runDestroyTest("Test E", subtask, WoodcuttersHut, WoodcuttersHut);
            SetupMPDTest(out store1, out store2, true); runDestroyTest("Test F", subtask, Forest, WoodcuttersHut);
        }
    }

    void runDestroyTest(string testName, int workerSubtask, BuildingData buildingToDestroy, BuildingData buildingToStoreItemIn)
    {
        BuildingData buildingWithItem = Forest;
        BuildingData buildingWorker = WoodcuttersHut;

        TestName = $"{testName}-{workerSubtask}: Destroy {buildingToDestroy.TestId} while {buildingWorker.TestId}'s worker is ";
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
        Forest.GatheringSpots.ForEach(s => s.PercentGrown = -float.MaxValue); // hack to ensure they don't grow

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        var spotWithWood = buildingWithItem.GetClosestUnreservedGatheringSpotWithItemToReap(worker.Location);
        var itemToBePickedUp = spotWithWood.ItemContainer.Item;
        var originalSpotWithItem = getGatheringSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var destroyedBuildingWithItemInIt = buildingWithItem == buildingToDestroy;
        var destroyedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToDestroy;
        var destroyedBuildingOfWorker = buildingWorker == buildingToDestroy;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        waitUntilTaskAndSubtaskIndex(worker, TaskType.GatherResource, workerSubtask > 5 ? workerSubtask - 2 : workerSubtask);

        var originalTask = worker.AI.CurrentTask as Task_GatherResource;
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface()} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 5)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage() + GetNumItemsInTownGatheringSpots();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;
        int origNumReservedStorageSpots = Town.Buildings.Sum(b => b.StorageSpots.Count(s => s.Reservation.IsReserved));
        int origNumReservedGatheringSpots = Town.Buildings.Sum(b => b.GatheringSpots.Count(s => s.Reservation.IsReserved));

        Town.DestroyBuilding(buildingToDestroy);

        // If the worker is returning with the item in hand or task was abandoned, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2 || originalTask.IsAbandoned)
            updateTown();

        // Verify new state.
        if (workerSubtask < 3)// WorkerSubtask_WalkToItemSpot, gather, and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);

            if (destroyedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.Idle, worker);
                verify_ItemsOnGround(1);
            }
            else if (destroyedBuildingOfWorker)
            {
                verify_spotIsUnreserved(originalSpotWithItem, $"{preface("", 1)} Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.Idle, worker, $"{preface("", 1)} Worker should be idle");
            }
            else // destroyedBuildingItemWillBeStoredIn
            {
                verify_spotIsReserved(originalSpotWithItem, $"{preface("", 1)} Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.GatherResource, worker);
                Assert.AreNotEqual(((Task_GatherResource)worker.AI.CurrentTask).SpotToStoreItemIn.Building, buildingToDestroy, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
            }
        }
        else if (workerSubtask < 6) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_ItemInSpot(originalSpotWithItem, null);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
            verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);

            if (destroyedBuildingWithItemInIt)
            {
                // by now we don't care; nothing should have changed
                verify_spotIsUnreserved(originalSpotWithItem, $"{preface("", 1)} Storage spot that originally contained the item should be unreserved");
                verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)} Storage spot that the item was originally going to be stored in should still be reserved");
                verify_WorkerTaskType(TaskType.GatherResource, worker);
                verify_ItemsOnGround(0);
            }
            else if (destroyedBuildingOfWorker)
            {
                verify_spotIsUnreserved(originalSpotWithItem, $"{preface("", 1)} Storage spot that originally contained the item should be unreserved");
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, $"{preface("", 1)} Worker should be carrying the already held item to storage");
            }
            else // destroyedBuildingItemWillBeStoredIn
            {
                verify_WorkerTaskType(TaskType.GatherResource, worker, $"{preface("", 1)} Worker should still be on the same task");
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                Assert.AreNotEqual(((Task_GatherResource)worker.AI.CurrentTask).SpotToStoreItemIn.Building, buildingToDestroy, $"{preface("", 1)} Worker should have reserved a spot in another building to store the item in");
            }
        }
        else // STORAGE FULL: WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot 
        {
            int newNumItemsInTownStorage = GetNumItemsInTownStorage() + GetNumItemsInTownGatheringSpots();
            int newNumItemsOnGround = Town.ItemsOnGround.Count;
            int newNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;
            int newNumReservedStorageSpots = Town.Buildings.Sum(b => b.StorageSpots.Count(s => s.Reservation.IsReserved));
            int newNumReservedGatheringSpots = Town.Buildings.Sum(b => b.GatheringSpots.Count(s => s.Reservation.IsReserved));

            verify_AssignedBuilding(worker, destroyedBuildingOfWorker ? Camp : workerOriginalAssignedBuilding);
            Assert.AreEqual(origNumItemsInTownStorage + origNumItemsOnGround + origNumItemsInWorkersHands, newNumItemsInTownStorage + newNumItemsOnGround + newNumItemsInWorkersHands, $"{preface("", 1)} Number of items in town (in storage+onground) should not have changed");

            if (destroyedBuildingWithItemInIt)
            {
                // nothing should have changed; we're already past the forest
                verify_WorkerTaskType(TaskType.GatherResource, worker, $"{preface("", 1)} Nothing should have changed");
                verify_spotIsReserved(originalSpotToStoreItemIn, $"{preface("", 1)} Nothing should have changed");
            }
            else
            {
                // if we destroyed the building to store in then worker can't deliver, so drops item to ground. in all other cases is still carrying it, but if
                // assigned building was destroyed then they're carrying to Camp now
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