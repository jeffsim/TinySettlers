using System.Linq;
using NUnit.Framework;
using UnityEngine;

public partial class WoodcutterHutTests : MovePauseDestroyTestBase
{
    [Test]
    public void WoodcutterHut_DestroyTests()
    {
        // subtask=0: woodcutter is walking to forest to gather wood to store in [buildingToStoreItemIn]
        // subtask=1: woodcutter is gathering wood in forest to store in [buildingToStoreItemIn]
        // subtask=2: woodcutter is picking up wood in forest to store in [buildingToStoreItemIn]
        // subtask=3: woodcutter is walking to [buildingToStoreItemIn]
        // subtask=4: woodcutter is dropping wood in [buildingToStoreItemIn]
        for (int subtask = 0; subtask < 6; subtask++)
        {
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
        Forest.GatheringSpots[0].PercentGrown = -1000; // hack to ensure they don't grow
        Forest.GatheringSpots[1].PercentGrown = -1000; // hack to ensure they don't grow
        Forest.GatheringSpots[2].PercentGrown = -1000; // hack to ensure they don't grow

        // Create the worker and wait until they get to the to-be-tested subtask
        var worker = Town.CreateWorkerInBuilding(buildingWorker);
        var spotWithWood = buildingWithItem.GetClosestUnreservedGatheringSpotWithItemToReap(worker.Location);
        var itemToBePickedUp = spotWithWood.ItemContainer.Item;
        var originalSpotWithItem = getGatheringSpotInBuildingWithItem(buildingWithItem, itemToBePickedUp);
        var destroyedBuildingWithItemInIt = buildingWithItem == buildingToDestroy;
        var destroyedBuildingItemWillBeStoredIn = buildingToStoreItemIn == buildingToDestroy;
        var destroyedBuildingOfWorker = buildingWorker == buildingToDestroy;
        var workerOriginalAssignedBuilding = worker.Assignment.AssignedTo;

        switch (workerSubtask)
        {
            case 0: waitUntilTaskAndSubtask(worker, TaskType.PickupGatherableResource, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 1: waitUntilTaskAndSubtask(worker, TaskType.PickupGatherableResource, typeof(WorkerSubtask_PickupItemFromBuilding)); break;
            case 2: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 3: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
            case 4: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_WalkToItemSpot)); break;
            case 5: waitUntilTaskAndSubtask(worker, TaskType.DeliverItemInHandToStorageSpot, typeof(WorkerSubtask_DropItemInItemSpot)); break;
        }
        var originalSpotToStoreItemIn = getStorageSpotInBuildingReservedByWorker(buildingToStoreItemIn, worker);
        Assert.IsNotNull(originalSpotToStoreItemIn, $"{preface()} Worker should have reserved a spot in {buildingToStoreItemIn.TestId} to store the item in");

        if (workerSubtask > 3)
            fillAllTownStorageWithItem("plank");
        int origNumItemsInTownStorage = GetNumItemsInTownStorage() + GetNumItemsInTownGatheringSpots();
        int origNumItemsOnGround = Town.ItemsOnGround.Count;
        int origNumItemsInWorkersHands = worker.Hands.HasItem ? 1 : 0;
        int origNumReservedStorageSpots = Town.Buildings.Sum(b => b.StorageSpots.Count(s => s.Reservation.IsReserved));
        int origNumReservedGatheringSpots = Town.Buildings.Sum(b => b.GatheringSpots.Count(s => s.Reservation.IsReserved));

        Town.DestroyBuilding(buildingToDestroy);

        // If the worker is returning with the item in hand, then we need to wait one Town turn so that the worker can decide to carry the item they're holding to the Camp.
        if (workerSubtask >= 2)
            updateTown();

        // Verify new state.
        if (workerSubtask == 0 || workerSubtask == 1)// WorkerSubtask_WalkToItemSpot and WorkerSubtask_PickupItemFromBuilding
        {
            verify_ItemDefnInHand(worker, null);
            if (destroyedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.Idle, worker);
                verify_ItemsOnGround(1);
            }
            else
            {
                verify_ItemInSpot(originalSpotWithItem, itemToBePickedUp);
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                if (destroyedBuildingOfWorker)
                {
                    verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                    verify_WorkerTaskType(TaskType.Idle, worker);
                }
                else if (destroyedBuildingItemWillBeStoredIn)
                {
                    verify_spotIsReserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");
                    verify_WorkerTaskType(TaskType.PickupGatherableResource, worker);
                    Assert.AreNotEqual(((WorkerTask_PickupGatherableResource)worker.AI.CurrentTask).ReservedSpotToStoreItemIn.Building, buildingToDestroy, $"{preface()} Worker should have reserved a spot in another building to store the item in");
                }
            }
        }
        else if (workerSubtask == 2 || workerSubtask == 3) // WorkerSubtask_WalkToItemSpot and WorkerSubtask_DropItemInItemSpot
        {
            verify_ItemInHand(worker, itemToBePickedUp);
            verify_ItemInSpot(originalSpotWithItem, null);
            verify_spotIsUnreserved(originalSpotWithItem, "Storage spot that originally contained the item should be unreserved");

            if (destroyedBuildingOfWorker)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker, "Should still be delivering the item that the worker is holding even though their building was destroyed");
                if (destroyedBuildingItemWillBeStoredIn)
                {
                    verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                    Assert.AreNotEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
                }
                else
                {
                    verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should still be reserved");
                    Assert.AreEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn.Building, $"{preface("", 1)} Worker should still have reserved the same spot to store the item in");
                }
            }
            if (destroyedBuildingWithItemInIt)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                verify_spotIsReserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be reserved");
            }
            if (destroyedBuildingItemWillBeStoredIn)
            {
                verify_WorkerTaskType(TaskType.DeliverItemInHandToStorageSpot, worker);
                verify_spotIsUnreserved(originalSpotToStoreItemIn, "Storage spot that item was going to be stored in should be unreserved");
                Assert.AreNotEqual(worker.StorageSpotReservedForItemInHand.Building, originalSpotToStoreItemIn, $"{preface("", 1)} Worker should have reserved a spot in a different building to store the item in");
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


            if (buildingToDestroy == originalSpotToStoreItemIn.Building)
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
// Test E-4: Destroy woodc while woodc's worker is walking to woodc to dropoff item picked up from forest and there are no available storage spots
//   StepNum 4, line 171:  Expected item in hand to be null, but is 'wood (2004)'
//   Expected: < wood(2004) >
//   But was: null